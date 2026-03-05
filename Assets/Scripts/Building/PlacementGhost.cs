using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementGhost : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _ghostRenderer;
    [SerializeField] private Color _validColor = new Color(0.3f, 1f, 0.3f, 0.5f);
    [SerializeField] private Color _invalidColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    public bool IsValid { get; private set; }

    private IPlacementContext _context;

    private void Awake()
    {
        if (_ghostRenderer == null)
            _ghostRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>Sets the placement context used for positioning and validation.</summary>
    public void SetContext(IPlacementContext context)
    {
        _context = context;
    }

    public void UpdateValidity(bool valid)
    {
        IsValid = valid;
        if (_ghostRenderer != null)
            _ghostRenderer.color = valid ? _validColor : _invalidColor;
    }

    private void Update()
    {
        if (Camera.main == null || Mouse.current == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
        worldPos.z = 0f;

        if (_context != null)
        {
            Vector3Int cellPos = _context.WorldToCell(worldPos);
            transform.position = _context.CellToWorldPosition(cellPos);
            UpdateValidity(_context.IsValidPosition(cellPos));
        }
        else
        {
            // Fallback: grid snap without validation
            Vector3Int gridPos = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
            transform.position = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f);

            bool walkable = CloudIsland.IsCurrentWalkable(gridPos);
            bool free = !PlacedItem.IsOccupied(gridPos);
            UpdateValidity(walkable && free);
        }
    }
}
