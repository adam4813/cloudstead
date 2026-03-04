using UnityEngine;

/// <summary>
/// Default placement context for the cloud surface.
/// Validates against TileManager walkability, uses grid+0.5 positioning.
/// </summary>
public class CloudPlacementContext : IPlacementContext
{
    private readonly Transform _parent;

    public CloudPlacementContext(Transform parent = null)
    {
        _parent = parent;
    }

    public bool IsValidPosition(Vector3Int cellPos)
    {
        bool walkable = TileManager.Instance != null && TileManager.Instance.IsWalkable(cellPos);
        return walkable && !PlacedItem.IsOccupied(cellPos);
    }

    public Vector3 CellToWorldPosition(Vector3Int cellPos)
    {
        return new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0f);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
    }

    public Transform GetParent() => _parent;

    public string SortingLayer => "Objects";
}
