using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [Tooltip("Y offset from transform center to sample the player's tile position (negative = lower)")]
    [SerializeField] private float feetOffsetY = -0.25f;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    // Multiplayer readiness — will be assigned when networking is added
    [System.NonSerialized] public uint ownerId;

    public Direction FacingDirection { get; private set; } = Direction.Down;
    public bool IsMoving { get; private set; }
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float BaseSpeed { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        BaseSpeed = moveSpeed;
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
}
