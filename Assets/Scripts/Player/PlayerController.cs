using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, ISaveable
{
    [SerializeField] private float moveSpeed = 4f;
    [Tooltip("Y offset from transform center to sample the player's tile position (negative = lower)")]
    [SerializeField] private float feetOffsetY = -0.25f;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    // Multiplayer readiness — will be assigned when networking is added
    [System.NonSerialized] public uint ownerId;

    /// <summary>Set by AirshipDock/AirshipController when the player boards/disembarks.</summary>
    public AirshipController CurrentAirship { get; set; }

    /// <summary>Set by InteriorManager when the player enters/exits a building.</summary>
    public BuildingInterior CurrentInterior { get; set; }

    /// <summary>Set by AirshipDock (clear on board) / AirshipController (set on disembark) / FarmStartSetup (initial).</summary>
    public CloudIsland CurrentCloud
    {
        get => _currentCloud;
        set
        {
            _currentCloud = value;
            CloudIsland.Current = value;
        }
    }
    private CloudIsland _currentCloud;

    public Direction FacingDirection { get; private set; } = Direction.Down;
    public bool IsMoving { get; private set; }
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float BaseSpeed { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        BaseSpeed = moveSpeed;
    }

    private void Start()
    {
        SaveManager.Instance?.Register(this);
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
        IsMoving = moveInput.sqrMagnitude > 0.01f;

        if (IsMoving)
            UpdateFacingDirection();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void UpdateFacingDirection()
    {
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            FacingDirection = moveInput.x > 0 ? Direction.Right : Direction.Left;
        else
            FacingDirection = moveInput.y > 0 ? Direction.Up : Direction.Down;
    }

    public Vector2 GetFacingVector()
    {
        return FacingDirection.DirectionToVector();
    }

    public Vector3Int GetTargetTile()
    {
        Vector2 facingOffset = GetFacingVector();
        Vector3 targetWorld = GetFeetPosition() + (Vector3)facingOffset;
        return targetWorld.WorldToTile();
    }

    public Vector3Int GetFeetTile()
    {
        return GetFeetPosition().WorldToTile();
    }

    public Vector3 GetFeetPosition()
    {
        return transform.position + new Vector3(0f, feetOffsetY, 0f);
    }

    public void FaceToward(Vector3Int targetTile)
    {
        Vector3Int playerTile = GetFeetTile();
        Vector2 delta = new Vector2(targetTile.x - playerTile.x, targetTile.y - playerTile.y);
        if (delta.sqrMagnitude < 0.01f) return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            FacingDirection = delta.x > 0 ? Direction.Right : Direction.Left;
        else
            FacingDirection = delta.y > 0 ? Direction.Up : Direction.Down;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryPlayBoundaryBump(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsMoving) return;
        TryPlayBoundaryBump(collision);
    }

    private void TryPlayBoundaryBump(Collision2D collision)
    {
        var boundary = collision.collider.GetComponentInParent<CloudBoundary>();
        if (boundary != null)
        {
            boundary.PlayBumpSound();
            boundary.PushBack(rb);
        }
    }

    public string SaveState()
    {
        var data = new PlayerSaveData
        {
            posX = transform.position.x,
            posY = transform.position.y,
            facing = (int)FacingDirection,
            context = ResolveContext()
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<PlayerSaveData>(json);
        if (data == null) return;

        var pos = new Vector3(data.posX, data.posY, 0f);
        FacingDirection = (Direction)data.facing;

        string ctx = data.context ?? "cloud:home";

        if (ctx.StartsWith("interior:"))
        {
            string buildingId = ctx.Substring("interior:".Length);
            foreach (var bi in FindObjectsByType<BuildingInterior>(FindObjectsSortMode.None))
            {
                if (bi.BuildingId == buildingId)
                {
                    InteriorManager.Instance?.EnterInterior(bi, pos);
                    // CurrentInterior is set by EnterInterior; find parent cloud
                    var cloud = bi.GetComponentInParent<CloudIsland>();
                    if (cloud != null) CurrentCloud = cloud;
                    return;
                }
            }
            Debug.LogWarning($"[PlayerController] RestoreState: interior '{buildingId}' not found, spawning at saved position");
        }
        else if (ctx.StartsWith("airship:"))
        {
            string id = ctx.Substring("airship:".Length);
            foreach (var ac in FindObjectsByType<AirshipController>(FindObjectsSortMode.None))
            {
                if (ac.AirshipId == id) { CurrentAirship = ac; break; }
            }
            transform.position = pos;
            return;
        }
        else if (ctx.StartsWith("cloud:"))
        {
            string id = ctx.Substring("cloud:".Length);
            foreach (var cg in FindObjectsByType<CloudIsland>(FindObjectsSortMode.None))
            {
                if (cg.CloudId == id) { CurrentCloud = cg; break; }
            }
        }

        transform.position = pos;
    }

    private string ResolveContext()
    {
        if (CurrentInterior != null && !string.IsNullOrEmpty(CurrentInterior.BuildingId))
            return $"interior:{CurrentInterior.BuildingId}";

        if (CurrentAirship != null)
            return $"airship:{CurrentAirship.AirshipId}";

        string cloudId = CurrentCloud != null ? CurrentCloud.CloudId : "home";
        return $"cloud:{cloudId}";
    }

    [System.Serializable]
    private class PlayerSaveData
    {
        public float posX, posY;
        public int facing;
        public string context;
    }
}
