using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class ResourceNodeSpawnConfig
{
    public ResourceNodeDefinition definition;
    public GameObject prefab;
    [Range(0, 20)] public int maxCount;
}

/// <summary>
/// Identity, data, and runtime context for a cloud island.
/// Holds tilemaps, walkability grid, resource configs, and placed-object containers.
/// Generation is delegated to a sibling CloudGenerator component.
/// </summary>
public class CloudIsland : MonoBehaviour, ISaveable
{
    [Header("Identity")]
    [SerializeField] private string cloudId = "home";
    [SerializeField] private string displayName = "Cloud Island";

    [Header("Size")]
    [SerializeField] private int cloudWidth = 30;
    [SerializeField] private int cloudHeight = 30;
    [SerializeField] private int seed;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap soilTilemap;
    [SerializeField] private Tilemap decorationTilemap;

    [Header("Obstacles")]
    [SerializeField] private ResourceNodeSpawnConfig[] nodeConfigs;
    [SerializeField] [Range(0f, 0.5f)] private float obstacleSafeZoneRadius = 0.08f;
    [SerializeField] private Transform obstacleParent;

    [Header("Placed Objects")]
    [Tooltip("Container whose children with PlacedItem components are auto-registered on start")]
    [SerializeField] private Transform placeableObjectsContainer;

    /// <summary>The island the player is currently on. Synced from PlayerController.CurrentCloud setter.</summary>
    public static CloudIsland Current { get; set; }

    private bool[,] walkabilityGrid;

    // ── Public Properties ────────────────────────────────────────────────────

    public string CloudId => cloudId;
    public string DisplayName => displayName;
    public int Width => cloudWidth;
    public int Height => cloudHeight;
    public int Seed { get => seed; set => seed = value; }
    public bool[,] WalkabilityGrid { get => walkabilityGrid; set => walkabilityGrid = value; }
    public Tilemap GroundTilemap => groundTilemap;
    public Tilemap SoilTilemap => soilTilemap;
    public Tilemap DecorationTilemap => decorationTilemap;
    public Transform ObstacleParent => obstacleParent;
    public Transform PlaceableObjectsContainer => placeableObjectsContainer;
    public ResourceNodeSpawnConfig[] NodeConfigs => nodeConfigs;

    /// <summary>Bottom-left tile coordinate. GO's position is the cloud centre.</summary>
    public Vector2Int TileOrigin => new Vector2Int(
        Mathf.RoundToInt(transform.position.x) - cloudWidth / 2,
        Mathf.RoundToInt(transform.position.y) - cloudHeight / 2);
    public Vector3Int Origin => new Vector3Int(TileOrigin.x, TileOrigin.y, 0);

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (seed == 0)
            seed = Random.Range(1, 99999);

        var generator = GetComponent<CloudGenerator>();
        if (generator != null)
            generator.Generate(this);

        ResourceNodeManager.Instance?.RegisterCloud(this);
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.SetDefaultParent(placeableObjectsContainer);
            PlacementManager.Instance.RegisterExistingItems(placeableObjectsContainer);
        }
        SaveManager.Instance?.Register(this);

        if (IslandRegistry.Instance != null)
        {
            IslandRegistry.Instance.RegisterIsland(new IslandInfo
            {
                cloudId = this.cloudId,
                displayName = this.displayName,
                worldCenter = (Vector2)transform.position,
                approximateRadius = Mathf.Max(cloudWidth, cloudHeight) / 2f,
                isHome = this.cloudId == "home"
            });
        }
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
        ResourceNodeManager.Instance?.UnregisterCloud(this);
    }

    // ── Walkability ──────────────────────────────────────────────────────────

    public bool IsWalkable(Vector3Int tilePos)
    {
        if (walkabilityGrid == null) return false;
        int x = tilePos.x - TileOrigin.x;
        int y = tilePos.y - TileOrigin.y;
        if (x < 0 || x >= cloudWidth || y < 0 || y >= cloudHeight)
            return false;
        return walkabilityGrid[x, y];
    }

    public bool IsEdgeTile(int x, int y)
    {
        if (walkabilityGrid == null) return false;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || nx >= cloudWidth || ny < 0 || ny >= cloudHeight)
                    return true;
                if (!walkabilityGrid[nx, ny])
                    return true;
            }
        }
        return false;
    }

    /// <summary>Context-aware walkability: checks interior floor when inside, else current cloud grid.</summary>
    public static bool IsCurrentWalkable(Vector3Int pos)
    {
        if (InteriorManager.Instance != null && InteriorManager.Instance.IsInsideInterior)
        {
            var interior = InteriorManager.Instance.CurrentInterior;
            return interior != null && interior.HasFloorTile(pos);
        }
        return Current != null && Current.IsWalkable(pos);
    }

    /// <summary>Context-aware edge check: always false inside interiors.</summary>
    public static bool IsCurrentEdgeTile(Vector3Int pos)
    {
        if (InteriorManager.Instance != null && InteriorManager.Instance.IsInsideInterior)
            return false;
        if (Current == null) return false;
        int x = pos.x - Current.TileOrigin.x;
        int y = pos.y - Current.TileOrigin.y;
        return Current.IsEdgeTile(x, y);
    }

    public bool HasSoilTile(Vector3Int pos)
    {
        return soilTilemap != null && soilTilemap.HasTile(pos);
    }

    public Vector3 GetCenterWorldPosition()
    {
        return transform.position + new Vector3(0.5f, 0.5f, 0f);
    }

    // ── Resource Node Queries ────────────────────────────────────────────────

    public List<Vector3> GetShuffledCandidateTiles()
    {
        var rng = new System.Random(seed);
        var origin = TileOrigin;
        float centerX = cloudWidth / 2f;
        float centerY = cloudHeight / 2f;
        float safeRadius = Mathf.Min(cloudWidth, cloudHeight) * obstacleSafeZoneRadius;

        var candidates = new List<Vector2Int>();
        for (int x = 0; x < cloudWidth; x++)
            for (int y = 0; y < cloudHeight; y++)
            {
                if (!walkabilityGrid[x, y]) continue;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (dist <= safeRadius) continue;
                candidates.Add(new Vector2Int(x, y));
            }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        var result = new List<Vector3>(candidates.Count);
        foreach (var c in candidates)
            result.Add(new Vector3(origin.x + c.x + 0.5f, origin.y + c.y + 0.5f, 0f));
        return result;
    }

    public int GetMaxCount(ResourceNodeDefinition def)
    {
        if (nodeConfigs == null) return 0;
        foreach (var c in nodeConfigs)
            if (c.definition == def) return c.maxCount;
        return 0;
    }

    public GameObject GetPrefab(ResourceNodeDefinition def)
    {
        if (nodeConfigs == null) return null;
        foreach (var c in nodeConfigs)
            if (c.definition == def) return c.prefab;
        return null;
    }

    public Vector3? GetRandomObstacleTile(System.Random rng)
    {
        if (walkabilityGrid == null) return null;
        var origin = TileOrigin;
        float centerX = cloudWidth / 2f;
        float centerY = cloudHeight / 2f;
        float safeRadius = Mathf.Min(cloudWidth, cloudHeight) * obstacleSafeZoneRadius;

        var candidates = new List<Vector2Int>();
        for (int x = 0; x < cloudWidth; x++)
            for (int y = 0; y < cloudHeight; y++)
            {
                if (!walkabilityGrid[x, y]) continue;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (dist <= safeRadius) continue;
                candidates.Add(new Vector2Int(x, y));
            }

        if (candidates.Count == 0) return null;
        var pick = candidates[rng.Next(candidates.Count)];
        return new Vector3(origin.x + pick.x + 0.5f, origin.y + pick.y + 0.5f, 0f);
    }

    // ── ISaveable ────────────────────────────────────────────────────────────

    public string SaveKey => $"CloudIsland:{cloudId}";

    public string SaveState()
    {
        return JsonUtility.ToJson(new CloudSaveData { seed = seed });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<CloudSaveData>(json);
        seed = data.seed;
        var generator = GetComponent<CloudGenerator>();
        if (generator != null)
            generator.Generate(this);
    }

    [System.Serializable]
    private class CloudSaveData
    {
        public int seed;
    }
}
