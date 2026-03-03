using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementGhost : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _ghostRenderer;
    [SerializeField] private Color _validColor = new Color(0.3f, 1f, 0.3f, 0.5f);
    [SerializeField] private Color _invalidColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    public bool IsValid { get; private set; }

    private void Awake()
    {
        if (_ghostRenderer == null)
            _ghostRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdatePosition(Vector3 worldPos)
    {
        Vector3Int gridPos = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
        transform.position = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f);
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

        Vector3Int gridPos = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
        transform.position = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f);

        bool walkable = TileManager.Instance != null && TileManager.Instance.IsWalkable(gridPos);
        bool free = !PlacedItem.IsOccupied(gridPos);
        UpdateValidity(walkable && free);
    }
}
