using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Runtime component for a building exterior constructed from tilemaps.
/// Lives on the root of the building prefab. References its BuildingDefinition SO
/// and optionally links to a BuildingInterior for enter/exit.
/// Implements IBuildable for grid-based placement validation.
///
/// Collision is handled by Unity's physics: add TilemapCollider2D to whichever
/// tilemap layers should block the player. Leave gaps for doors/passages.
/// Bridges simply omit colliders on their walkable surface layer.
/// </summary>
public class BuildingStructure : MonoBehaviour, IBuildable, ISaveable
{
    [FoldoutGroup("Definition")]
    [Required]
    [SerializeField] private BuildingDefinition definition;
    public BuildingDefinition Definition => definition;

    [FoldoutGroup("Tilemaps")]
    [Tooltip("All exterior tilemap layers (walls, roof, floor, decoration). Collision is driven by TilemapCollider2D on individual layers, not by this array.")]
    [SerializeField] private Tilemap[] exteriorTilemaps;

    [FoldoutGroup("Interior")]
    [Tooltip("Optional interior. Null for exterior-only structures (wells, shrines, bridges).")]
    [SerializeField] private BuildingInterior interior;
    public BuildingInterior Interior => interior;

    [FoldoutGroup("Interior")]
    [Tooltip("The door trigger connecting exterior to interior. Auto-discovered from children if null.")]
    [SerializeField] private DoorTrigger doorTrigger;

    [FoldoutGroup("Placement")]
    [Tooltip("Offset from the grid origin to the visual center. Adjusts where the prefab sits relative to the anchor tile.")]
    [SerializeField] private Vector2 visualOffset;

    private Vector3Int _gridOrigin;
    private bool _isPlaced;

    public string BuildingId => definition != null ? definition.buildingName : gameObject.name;

    // ── IBuildable ───────────────────────────────────────────────────────────

    public Vector2Int GridSize
    {
        get
        {
            var bounds = GetExteriorBounds();
            return new Vector2Int(
                Mathf.Max(1, Mathf.CeilToInt(bounds.size.x)),
                Mathf.Max(1, Mathf.CeilToInt(bounds.size.y))
            );
        }
    }

    public bool CanPlaceAt(Vector3Int gridPosition)
    {
        Vector2Int size = GridSize;
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                var cell = new Vector3Int(gridPosition.x + x, gridPosition.y + y, 0);
                if (!CloudIsland.IsCurrentWalkable(cell) || PlacedItem.IsOccupied(cell))
                    return false;
            }
        }
        return true;
    }

    public void OnPlaced(Vector3Int gridPosition)
    {
        _gridOrigin = gridPosition;
        _isPlaced = true;

        Vector3 worldPos = new Vector3(
            gridPosition.x + visualOffset.x,
            gridPosition.y + visualOffset.y,
            0f
        );
        transform.position = worldPos;
    }

    public void OnRemoved()
    {
        _isPlaced = false;
        Destroy(gameObject);
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (doorTrigger == null)
            doorTrigger = GetComponentInChildren<DoorTrigger>();
    }

    /// <summary>
    /// Returns the world-space bounds of all exterior tilemaps combined.
    /// Useful for camera framing and placement previews.
    /// </summary>
    public Bounds GetExteriorBounds()
    {
        var bounds = new Bounds(transform.position, Vector3.zero);
        bool first = true;
        foreach (var tm in exteriorTilemaps)
        {
            if (tm == null) continue;
            tm.CompressBounds();
            var worldMin = tm.transform.TransformPoint(tm.localBounds.min);
            var worldMax = tm.transform.TransformPoint(tm.localBounds.max);
            var wb = new Bounds();
            wb.SetMinMax(worldMin, worldMax);
            if (first) { bounds = wb; first = false; }
            else bounds.Encapsulate(wb);
        }
        return bounds;
    }

    // ── ISaveable ────────────────────────────────────────────────────────────

    public string SaveKey => $"Building:{BuildingId}";

    public string SaveState()
    {
        return JsonUtility.ToJson(new BuildingSaveData
        {
            gridOriginX = _gridOrigin.x,
            gridOriginY = _gridOrigin.y,
            isPlaced = _isPlaced
        });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<BuildingSaveData>(json);
        if (data.isPlaced)
            OnPlaced(new Vector3Int(data.gridOriginX, data.gridOriginY, 0));
    }

    [System.Serializable]
    private class BuildingSaveData
    {
        public int gridOriginX;
        public int gridOriginY;
        public bool isPlaced;
    }
}
