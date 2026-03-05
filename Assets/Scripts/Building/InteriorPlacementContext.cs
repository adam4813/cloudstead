using UnityEngine;

/// <summary>
/// Placement context for building interiors.
/// Uses interior sorting layers and parents items to the interior's ObjectsContainer.
/// </summary>
public class InteriorPlacementContext : IPlacementContext
{
    private readonly BuildingInterior _interior;

    public InteriorPlacementContext(BuildingInterior interior)
    {
        _interior = interior;
    }

    public bool IsValidPosition(Vector3Int cellPos)
    {
        bool walkable = CloudIsland.IsCurrentWalkable(cellPos);
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

    public Transform GetParent()
    {
        if (_interior == null) return null;
        return _interior.PlaceableObjectsContainer != null
            ? _interior.PlaceableObjectsContainer
            : _interior.ObjectsContainer;
    }

    public string SortingLayer => InteriorManager.ToInteriorLayer("Objects");
}
