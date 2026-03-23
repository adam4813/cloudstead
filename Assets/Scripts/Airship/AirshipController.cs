using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>
/// Controls airship movement and manages player boarding/disembarking.
///
/// The airship body is a Grid + Tilemap hierarchy (see prefab), making it
/// ready for a future tile-based building system. This controller only
/// cares about Rigidbody2D — it works with any tilemap shape.
///
/// Movement is hovercraft-style (omni-directional, no rotation) since
/// rotating a tilemap looks odd in top-down 2D. When the building system
/// is added, the Rigidbody2D mass/drag can be recalculated based on tile count.
///
/// Input: uses the player's PlayerInput (switched to Airship map by
/// GameStateController). Callbacks are wired in BoardPlayer() and
/// unwired in DisembarkPlayer() — no second PlayerInput needed.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AirshipController : MonoBehaviour, ISaveable, IFarmingContext
{
    [Header("Identity")]
    [SerializeField] private string airshipId = "player";
    public string AirshipId => airshipId;

    [Header("Ownership")]
    [Tooltip("0 = unowned. Only the owner can pilot this airship.")]
    [SerializeField] private uint ownerId;
    public uint OwnerId { get => ownerId; set => ownerId = value; }

    [Header("Movement")]
    [SerializeField] private float thrustForce = 8f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float linearDrag = 1.5f;
    [SerializeField] private float turnSpeed = 90f; // degrees per second

    [Header("Boarding")]
    [SerializeField] private Transform helmPosition; // default from prefab
    [SerializeField] private Transform disembarkOffset;

    [Header("Farming")]
    [Tooltip("Optional soil tilemap for deck flowerbeds. Leave null if no farming on this airship.")]
    [SerializeField] private Tilemap soilTilemap;
    [Tooltip("Floor tilemap used for walkability checks (typically from AirshipBuildMode).")]
    [SerializeField] private Tilemap floorTilemap;

    private Transform _helmOverride;

    /// <summary>Active helm: player-placed override if set, otherwise the prefab default.</summary>
    private Transform ActiveHelm => _helmOverride != null ? _helmOverride : helmPosition;

    /// <summary>Returns the active helm transform for external callers (e.g., CutscenePlayer passenger boarding).</summary>
    public Transform GetHelmTransform() => ActiveHelm;

    /// <summary>
    /// Called by AirshipHelm to register a player-placed helm as the active position.
    /// Pass null to revert to the default prefab helm.
    /// </summary>
    public void SetHelmPosition(Transform pos) => _helmOverride = pos;

    [Header("Audio")]
    [SerializeField] private AudioSource engineHumSource;
    [SerializeField] private AudioClip landChime;

    private Rigidbody2D rb;
    private Vector2 thrustInput;
    private PlayerController boardedPlayer;
    private PlayerInput boardedPlayerInput;
    private Rigidbody2D boardedPlayerRb;
    private Collider2D[] boardedPlayerColliders;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = linearDrag;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.None; // rotation needed for steering
        rb.bodyType = RigidbodyType2D.Kinematic; // docked by default
    }

    private void FixedUpdate()
    {
        if (!IsFlying) return;

        // Pause flight input while overlay UI is open (inventory, map)
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Airship)
        {
            thrustInput = Vector2.zero;
            return;
        }

        // A/D: rotate the ship directly (responsive steering, no torque physics)
        if (thrustInput.x != 0f)
            rb.MoveRotation(rb.rotation - thrustInput.x * turnSpeed * Time.fixedDeltaTime);

        // Kill any angular velocity from collisions — rotation is manual only
        rb.angularVelocity = 0f;

        // W/S: thrust along the ship's forward axis (transform.up in top-down 2D)
        if (thrustInput.y != 0f)
            rb.AddForce(transform.up * thrustInput.y * thrustForce);

        // Clamp speed — coasting handled by linearDamping
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // Pin player to helm each physics step — avoids nested Rigidbody2D issues
        if (boardedPlayer != null && ActiveHelm != null)
            boardedPlayer.transform.position = ActiveHelm.position;
    }

    /// <summary>Called by AirshipHelm when the player interacts with the helm.</summary>
    public void BoardPlayer(PlayerController player)
    {
        boardedPlayer = player;
        boardedPlayerInput = player.GetComponent<PlayerInput>();
        boardedPlayerRb = player.GetComponent<Rigidbody2D>();

        // Wire Airship map callbacks using explicit map/action lookup with null guards
        var thrustAction = boardedPlayerInput?.actions.FindActionMap("Airship")?.FindAction("Thrust");
        var landAction   = boardedPlayerInput?.actions.FindActionMap("Airship")?.FindAction("Land");

        if (thrustAction == null || landAction == null)
        {
            Debug.LogError("[AirshipController] Could not find Airship/Thrust or Airship/Land actions. Check the Input Action Asset.");
            return;
        }

        thrustAction.performed += OnThrust;
        thrustAction.canceled  += OnThrustCanceled;
        landAction.performed   += OnLand;

        // Disable the player's own Rigidbody2D simulation —
        // nested Rigidbody2Ds fight each other in Unity 2D physics.
        if (boardedPlayerRb != null)
        {
            boardedPlayerRb.linearVelocity = Vector2.zero;
            boardedPlayerRb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable player colliders — an active collider inside the airship's
        // composite collider pushes back against the Dynamic Rigidbody2D,
        // preventing the airship from moving.
        boardedPlayerColliders = boardedPlayer.GetComponents<Collider2D>();
        foreach (var col in boardedPlayerColliders)
            col.enabled = false;

        // Teleport to helm — FixedUpdate pins the position each frame.
        // Player may already be parented to the airship from AirshipDock boarding.
        if (ActiveHelm != null)
            boardedPlayer.transform.position = ActiveHelm.position;

        // Become dynamic (unmoored)
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;

        if (engineHumSource != null)
            engineHumSource.Play();

        EventBus.Publish(new AirshipBoardedEvent { AirshipTransform = transform });
    }

    /// <summary>Called by OnLand when a valid landing pad is found.</summary>
    public void DisembarkPlayer(Vector2 spawnPosition, bool moorAtDock, CloudIsland landingCloud = null)
    {
        if (boardedPlayer == null) return;

        // Unwire input callbacks
        var thrustAction = boardedPlayerInput?.actions.FindActionMap("Airship")?.FindAction("Thrust");
        var landAction   = boardedPlayerInput?.actions.FindActionMap("Airship")?.FindAction("Land");

        if (thrustAction != null)
        {
            thrustAction.performed -= OnThrust;
            thrustAction.canceled  -= OnThrustCanceled;
        }
        if (landAction != null)
            landAction.performed -= OnLand;

        // Return player to world
        boardedPlayer.transform.SetParent(null);
        boardedPlayer.transform.position = spawnPosition;

        // Restore player's Rigidbody2D and colliders
        if (boardedPlayerRb != null)
            boardedPlayerRb.bodyType = RigidbodyType2D.Dynamic;
        if (boardedPlayerColliders != null)
        {
            foreach (var col in boardedPlayerColliders)
                col.enabled = true;
            boardedPlayerColliders = null;
        }

        thrustInput = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; // always freeze on disembark

        if (landChime != null)
            AudioSource.PlayClipAtPoint(landChime, transform.position);

        if (engineHumSource != null)
            engineHumSource.Stop();

        boardedPlayer.CurrentAirship = null;
        boardedPlayer.CurrentCloud = landingCloud;

        // Revert placement context from airship to default
        PlacementManager.Instance?.SetContext(null);

        boardedPlayer = null;
        boardedPlayerInput = null;
        boardedPlayerRb = null;

        EventBus.Publish(new AirshipLandedEvent { CloudName = "Farm" });
        GameManager.Instance.SetState(GameState.Playing);
    }

    public bool HasBoardedPlayer => boardedPlayer != null;
    public bool IsFlying => rb.bodyType == RigidbodyType2D.Dynamic;

    private void OnThrust(InputAction.CallbackContext ctx)
    {
        thrustInput = ctx.ReadValue<Vector2>();
    }

    private void OnThrustCanceled(InputAction.CallbackContext ctx)
    {
        thrustInput = Vector2.zero;
    }

    private void OnLand(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        LandingPad pad = SkyWorldManager.Instance?.GetNearestLandablePad(transform.position);
        if (pad != null)
        {
            bool isDock = pad.GetComponent<AirshipDock>() != null;
            // Resolve the cloud this pad belongs to so the player's context updates
            var cloud = pad.GetComponentInParent<CloudIsland>();
            DisembarkPlayer(pad.GetPlayerSpawnPosition(), isDock, cloud);
        }
        else
        {
            // No pad nearby — stop piloting, player stands on deck
            Vector2 spawnPos = disembarkOffset != null
                ? (Vector2)disembarkOffset.position
                : (Vector2)transform.position;
            DisembarkPlayer(spawnPos, false, null);
        }
    }

    // ── IFarmingContext ──────────────────────────────────────────────────────

    public string ContextId => $"airship:{airshipId}";
    Tilemap IFarmingContext.SoilTilemap => soilTilemap;

    bool IFarmingContext.IsWalkable(Vector3Int tilePos)
    {
        if (floorTilemap == null) return false;
        Vector3 worldPos = new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0f);
        Vector3Int cellPos = floorTilemap.WorldToCell(worldPos);
        return floorTilemap.HasTile(cellPos);
    }

    #region ISaveable

    public string SaveKey => $"AirshipController:{airshipId}";

    private void Start()
    {
        SaveManager.Instance?.Register(this);
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
    }

    public string SaveState()
    {
        return JsonUtility.ToJson(new AirshipSaveData
        {
            posX = transform.position.x,
            posY = transform.position.y,
            rotationZ = transform.eulerAngles.z,
            ownerId = ownerId
        });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<AirshipSaveData>(json);
        if (data == null) return;

        transform.position = new Vector3(data.posX, data.posY, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, data.rotationZ);
        ownerId = data.ownerId;

        // Ensure docked state on load
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    [System.Serializable]
    private class AirshipSaveData
    {
        public float posX;
        public float posY;
        public float rotationZ;
        public uint ownerId;
    }

    #endregion
}
