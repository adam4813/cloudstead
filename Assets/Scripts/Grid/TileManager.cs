using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : Singleton<TileManager>
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap soilTilemap;
    [SerializeField] private Tilemap decorationTilemap;
    [SerializeField] private CloudGenerator cloudGenerator;

    public Tilemap GroundTilemap => groundTilemap;
    public Tilemap SoilTilemap => soilTilemap;
    public Tilemap DecorationTilemap => decorationTilemap;

    public TileBase GetTileAt(Vector3Int pos, Tilemap layer = null)
    {
        layer = layer != null ? layer : groundTilemap;
        return layer.GetTile(pos);
    }

    public bool IsWalkable(Vector3Int pos)
    {
        // When inside an interior, check the interior floor tilemap instead
        if (InteriorManager.Instance != null && InteriorManager.Instance.IsInsideInterior)
        {
            var interior = InteriorManager.Instance.CurrentInterior;
            return interior != null && interior.HasFloorTile(pos);
        }

        if (cloudGenerator != null)
            return cloudGenerator.IsWalkable(pos);

        return groundTilemap != null && groundTilemap.HasTile(pos);
    }

    public void SetTile(Vector3Int pos, TileBase tile, Tilemap layer)
    {
        if (layer != null)
            layer.SetTile(pos, tile);
    }

    public bool HasSoilTile(Vector3Int pos)
    {
        return soilTilemap != null && soilTilemap.HasTile(pos);
    }

    public bool IsEdgeTile(Vector3Int pos)
    {
        if (InteriorManager.Instance != null && InteriorManager.Instance.IsInsideInterior)
            return false;
        return cloudGenerator != null && cloudGenerator.IsEdgeTile(pos.x, pos.y);
    }
}
