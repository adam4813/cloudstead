using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Placement context for airship decks.
/// Validates items against the airship's floor tilemap, positions using tilemap cell centers,
/// and parents items to the airship transform so they move with the ship.
/// </summary>
public class AirshipPlacementContext : IPlacementContext
{
    private readonly Transform _airshipTransform;
    private readonly Tilemap _floorTilemap;

    public AirshipPlacementContext(Transform airshipTransform, Tilemap floorTilemap)
    {
        _airshipTransform = airshipTransform;
        _floorTilemap = floorTilemap;
    }

    public bool IsValidPosition(Vector3Int cellPos)
    {
        if (_floorTilemap == null) return false;
        if (_floorTilemap.GetTile(cellPos) == null) return false;
        return !PlacedItem.IsOccupied(cellPos);
    }

    public Vector3 CellToWorldPosition(Vector3Int cellPos)
    {
        if (_floorTilemap != null)
            return _floorTilemap.GetCellCenterWorld(cellPos);
        return new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0f);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        if (_floorTilemap != null)
            return _floorTilemap.WorldToCell(worldPos);
        return new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
    }

    public Transform GetParent() => _airshipTransform;

    public string SortingLayer => "Objects";
}
