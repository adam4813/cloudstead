using UnityEngine;
using UnityEngine.InputSystem;

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
public class AirshipController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float thrustSpeed = 8f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float linearDrag = 2f;

    [Header("Boarding")]
    [SerializeField] private Transform helmPosition;

    [Header("Audio")]
    [SerializeField] private AudioSource engineHumSource;
    [SerializeField] private AudioClip landChime;

    private Rigidbody2D rb;
    private Vector2 thrustInput;
    private PlayerController boardedPlayer;
    private PlayerInput boardedPlayerInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = linearDrag;
        rb.angularDamping = 10f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic; // docked by default
    }

    private void FixedUpdate()
    {
        if (rb.bodyType != RigidbodyType2D.Dynamic) return;

        rb.AddForce(thrustInput.normalized * thrustSpeed);

        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    /// <summary>Called by AirshipDock when the player interacts with the dock.</summary>
    public void BoardPlayer(PlayerController player)
    {
        boardedPlayer = player;
        boardedPlayerInput = player.GetComponent<PlayerInput>();

        // Manually wire Airship map callbacks — no second PlayerInput on the airship
        boardedPlayerInput.actions["Airship/Thrust"].performed += OnThrust;
        boardedPlayerInput.actions["Airship/Thrust"].canceled += OnThrustCanceled;
        boardedPlayerInput.actions["Airship/Land"].performed += OnLand;

        // Parent player to airship — sprite stays visible at helm
        player.transform.SetParent(transform);
        if (helmPosition != null)
            player.transform.position = helmPosition.position;

        // Become dynamic (unmoored)
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;

        if (engineHumSource != null)
            engineHumSource.Play();

        EventBus.Publish(new AirshipBoardedEvent { AirshipTransform = transform });
    }

    /// <summary>Called by OnLand when a valid landing pad is found.</summary>
    public void DisembarkPlayer(Vector2 spawnPosition, bool moorAtDock)
    {
        if (boardedPlayer == null) return;

        // Unwire input callbacks
        boardedPlayerInput.actions["Airship/Thrust"].performed -= OnThrust;
        boardedPlayerInput.actions["Airship/Thrust"].canceled -= OnThrustCanceled;
        boardedPlayerInput.actions["Airship/Land"].performed -= OnLand;

        // Return player to world
        boardedPlayer.transform.SetParent(null);
        boardedPlayer.transform.position = spawnPosition;

        thrustInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        if (moorAtDock)
            rb.bodyType = RigidbodyType2D.Kinematic; // re-dock

        if (landChime != null)
            AudioSource.PlayClipAtPoint(landChime, transform.position);

        if (engineHumSource != null)
            engineHumSource.Stop();

        boardedPlayer = null;
        boardedPlayerInput = null;

        GameManager.Instance.SetState(GameState.Playing);
    }

    public bool HasBoardedPlayer => boardedPlayer != null;

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
        if (pad == null) return;

        bool isDock = pad.GetComponent<AirshipDock>() != null;
        DisembarkPlayer(pad.GetPlayerSpawnPosition(), isDock);
    }
}
