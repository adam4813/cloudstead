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

public class CloudGenerator : MonoBehaviour, ISaveable
{
    [SerializeField] private string cloudId = "home";
    public string CloudId => cloudId;

    [SerializeField] private int cloudWidth = 30;
    [SerializeField] private int cloudHeight = 30;
    [SerializeField] private float noiseScale = 0.15f;
    [SerializeField] private float threshold = 0.4f;
    [SerializeField] private int seed;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private TileBase cloudTile;
    [SerializeField] private TileBase edgeTile;

    [Header("Obstacles")]
    [SerializeField] private ResourceNodeSpawnConfig[] nodeConfigs;
    [SerializeField] [Range(0f, 0.5f)] private float obstacleSafeZoneRadius = 0.08f;
    [SerializeField] private Transform obstacleParent;

    private bool[,] walkabilityGrid;

    /// <summary>Bottom-left tile coordinate. GO's position is the cloud centre.</summary>
    public Vector2Int TileOrigin => new Vector2Int(
        Mathf.RoundToInt(transform.position.x) - cloudWidth / 2,
        Mathf.RoundToInt(transform.position.y) - cloudHeight / 2);
    public Vector3Int Origin => new Vector3Int(TileOrigin.x, TileOrigin.y, 0);
    public bool[,] WalkabilityGrid => walkabilityGrid;
    public int Width => cloudWidth;
    public int Height => cloudHeight;
    public Transform ObstacleParent => obstacleParent;
    public ResourceNodeSpawnConfig[] NodeConfigs => nodeConfigs;

    private void Start()
    {
        if (seed == 0)
            seed = Random.Range(1, 99999);

        Generate();
        ResourceNodeManager.Instance?.RegisterCloud(this);
        SaveManager.Instance?.Register(this);
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
        ResourceNodeManager.Instance?.UnregisterCloud(this);
    }

    public void Generate()
    {
        GenerateTerrain();
        ClearObstacles();
    }

    private void GenerateTerrain()
    {
        walkabilityGrid = new bool[cloudWidth, cloudHeight];

        int centerX = cloudWidth / 2;
        int centerY = cloudHeight / 2;
        float maxRadius = Mathf.Min(cloudWidth, cloudHeight) * 0.45f;

        for (int x = 0; x < cloudWidth; x++)
        {
            for (int y = 0; y < cloudHeight; y++)
            {
                float noise = Mathf.PerlinNoise(
                    (x + seed) * noiseScale,
                    (y + seed) * noiseScale);

                float distFromCenter = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(centerX, centerY));

                float falloff = 1f - Mathf.Clamp01(distFromCenter / maxRadius);

                // Center area always solid (safe spawn)
                float centerBonus = distFromCenter < maxRadius * 0.3f ? 0.3f : 0f;

                float value = noise * falloff + centerBonus;
                walkabilityGrid[x, y] = value > threshold;
            }
        }

        SmoothEdges();
        PaintTilemap();

        // Rebuild boundary colliders if they exist
        var boundary = GetComponent<CloudBoundary>();
        if (boundary != null)
            boundary.RegenerateBoundary();
    }

    private void SmoothEdges()
    {
        var smoothed = new bool[cloudWidth, cloudHeight];
        for (int x = 0; x < cloudWidth; x++)
        {
            for (int y = 0; y < cloudHeight; y++)
            {
                int neighbors = CountNeighbors(x, y);
                smoothed[x, y] = neighbors >= 4;
            }
        }
        walkabilityGrid = smoothed;
    }

    private int CountNeighbors(int x, int y)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < cloudWidth && ny >= 0 && ny < cloudHeight)
                {
                    if (walkabilityGrid[nx, ny]) count++;
                }
                else
                {
                    // Out of bounds counts as empty
                }
            }
        }
        return count;
    }

    private void PaintTilemap()
    {
        if (groundTilemap == null) return;

        groundTilemap.ClearAllTiles();

        int ox = TileOrigin.x;
        int oy = TileOrigin.y;

        for (int x = 0; x < cloudWidth; x++)
        {
            for (int y = 0; y < cloudHeight; y++)
            {
                if (!walkabilityGrid[x, y]) continue;

                var pos = new Vector3Int(ox + x, oy + y, 0);
                bool isEdge = IsEdgeTile(x, y);
                groundTilemap.SetTile(pos, isEdge ? (edgeTile ?? cloudTile) : cloudTile);
            }
        }
    }

    public bool IsEdgeTile(int x, int y)
    {
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

    private void ClearObstacles()
    {
        if (obstacleParent != null)
        {
            for (int i = obstacleParent.childCount - 1; i >= 0; i--)
                DestroyImmediate(obstacleParent.GetChild(i).gameObject);
        }
        else
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.GetComponent<ResourceNode>() != null)
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    /// <summary>Returns all valid obstacle spawn positions in seeded-shuffled order for ResourceNodeManager to consume.</summary>
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

    /// <summary>Returns a random world position for a new obstacle, or null if none available.</summary>
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

    public bool IsWalkable(Vector3Int tilePos)
    {
        int x = tilePos.x - TileOrigin.x;
        int y = tilePos.y - TileOrigin.y;
        if (x < 0 || x >= cloudWidth || y < 0 || y >= cloudHeight)
            return false;
        return walkabilityGrid[x, y];
    }

    public Vector3 GetCenterWorldPosition()
    {
        return transform.position + new Vector3(0.5f, 0.5f, 0f);
    }

    #region ISaveable

    public string SaveKey => $"CloudGenerator:{cloudId}";

    public string SaveState()
    {
        return JsonUtility.ToJson(new CloudSaveData { seed = seed });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<CloudSaveData>(json);
        seed = data.seed;
        GenerateTerrain();
    }

    [System.Serializable]
    private class CloudSaveData
    {
        public int seed;
    }

    #endregion
}
