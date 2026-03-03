using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CloudGenerator : MonoBehaviour
{
    [SerializeField] private int cloudWidth = 30;
    [SerializeField] private int cloudHeight = 30;
    [SerializeField] private float noiseScale = 0.15f;
    [SerializeField] private float threshold = 0.4f;
    [SerializeField] private int seed;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private TileBase cloudTile;
    [SerializeField] private TileBase edgeTile;

    [Header("Obstacles")]
    [SerializeField] private GameObject[] treePrefabs;
    [SerializeField] private GameObject[] rockPrefabs;
    [SerializeField] [Range(0, 20)] private int treeCount = 5;
    [SerializeField] [Range(0, 10)] private int rockCount = 3;
    [SerializeField] private Transform obstacleParent;

    private bool[,] walkabilityGrid;

    public bool[,] WalkabilityGrid => walkabilityGrid;
    public int Width => cloudWidth;
    public int Height => cloudHeight;
    public Vector3Int Origin => Vector3Int.zero;

    private void Start()
    {
        if (seed == 0)
            seed = Random.Range(1, 99999);

        Generate();
    }

    public void Generate()
    {
        ClearObstacles();
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
        SpawnObstacles();
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

        for (int x = 0; x < cloudWidth; x++)
        {
            for (int y = 0; y < cloudHeight; y++)
            {
                if (!walkabilityGrid[x, y]) continue;

                var pos = new Vector3Int(x, y, 0);
                bool isEdge = IsEdgeTile(x, y);
                groundTilemap.SetTile(pos, isEdge ? (edgeTile ?? cloudTile) : cloudTile);
            }
        }
    }

    private bool IsEdgeTile(int x, int y)
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
                if (child.name.StartsWith("Tree") || child.name.StartsWith("Rock"))
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    private void SpawnObstacles()
    {
        bool hasTrees = treePrefabs != null && treePrefabs.Length > 0;
        bool hasRocks = rockPrefabs != null && rockPrefabs.Length > 0;
        if (!hasTrees && !hasRocks) return;

        var rng = new System.Random(seed);

        float centerX = cloudWidth / 2f;
        float centerY = cloudHeight / 2f;
        float safeRadius = Mathf.Min(cloudWidth, cloudHeight) * 0.25f;

        var candidates = new List<Vector2Int>();
        for (int x = 0; x < cloudWidth; x++)
        {
            for (int y = 0; y < cloudHeight; y++)
            {
                if (!walkabilityGrid[x, y]) continue;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (dist <= safeRadius) continue;
                candidates.Add(new Vector2Int(x, y));
            }
        }

        // Fisher-Yates shuffle
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        Transform parent = obstacleParent != null ? obstacleParent : transform;
        int index = 0;

        if (hasTrees)
        {
            int spawned = 0;
            while (spawned < treeCount && index < candidates.Count)
            {
                var pos = candidates[index++];
                var prefab = treePrefabs[rng.Next(treePrefabs.Length)];
                var go = Instantiate(prefab, new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f), Quaternion.identity, parent);
                go.name = $"Tree_{spawned}";
                spawned++;
            }
        }

        if (hasRocks)
        {
            int spawned = 0;
            while (spawned < rockCount && index < candidates.Count)
            {
                var pos = candidates[index++];
                var prefab = rockPrefabs[rng.Next(rockPrefabs.Length)];
                var go = Instantiate(prefab, new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f), Quaternion.identity, parent);
                go.name = $"Rock_{spawned}";
                spawned++;
            }
        }
    }

    public bool IsWalkable(Vector3Int tilePos)
    {
        int x = tilePos.x;
        int y = tilePos.y;
        if (x < 0 || x >= cloudWidth || y < 0 || y >= cloudHeight)
            return false;
        return walkabilityGrid[x, y];
    }

    public Vector3 GetCenterWorldPosition()
    {
        return new Vector3(cloudWidth / 2f + 0.5f, cloudHeight / 2f + 0.5f, 0f);
    }
}
