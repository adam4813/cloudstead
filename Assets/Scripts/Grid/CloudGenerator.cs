using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Procedural cloud terrain generation algorithm.
/// Populates a CloudIsland's walkability grid and paints its tilemap.
/// Lives as a sibling component on the same GameObject as CloudIsland.
/// </summary>
public class CloudGenerator : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] private float noiseScale = 0.15f;
    [SerializeField] private float threshold = 0.4f;

    [Header("Tiles")]
    [SerializeField] private TileBase cloudTile;
    [SerializeField] private TileBase edgeTile;

    /// <summary>Generates terrain for the given island: walkability grid, tilemap painting, boundary rebuild.</summary>
    public void GenerateTerrain(CloudIsland island)
    {
        int w = island.Width;
        int h = island.Height;
        var grid = new bool[w, h];

        int centerX = w / 2;
        int centerY = h / 2;
        float maxRadius = Mathf.Min(w, h) * 0.45f;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                float noise = Mathf.PerlinNoise(
                    (x + island.Seed) * noiseScale,
                    (y + island.Seed) * noiseScale);

                float distFromCenter = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(centerX, centerY));

                float falloff = 1f - Mathf.Clamp01(distFromCenter / maxRadius);
                float centerBonus = distFromCenter < maxRadius * 0.3f ? 0.3f : 0f;
                float value = noise * falloff + centerBonus;
                grid[x, y] = value > threshold;
            }
        }

        // Smooth edges
        var smoothed = new bool[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                smoothed[x, y] = CountNeighbors(grid, x, y, w, h) >= 4;

        island.WalkabilityGrid = smoothed;
        PaintTilemap(island);
    }

    private int CountNeighbors(bool[,] grid, int x, int y, int w, int h)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < w && ny >= 0 && ny < h && grid[nx, ny])
                    count++;
            }
        return count;
    }

    private void PaintTilemap(CloudIsland island)
    {
        var tilemap = island.GroundTilemap;
        if (!tilemap) return;

        tilemap.ClearAllTiles();

        // Tiles are placed in Grid-local cell space.
        // The Grid is a child of the cloud GO, so cell (0,0) = cloud GO world position.
        // Offset by -width/2, -height/2 to center the cloud on the GO.
        int ox = -island.Width / 2;
        int oy = -island.Height / 2;
        var grid = island.WalkabilityGrid;

        for (int x = 0; x < island.Width; x++)
        {
            for (int y = 0; y < island.Height; y++)
            {
                if (!grid[x, y]) continue;
                var pos = new Vector3Int(ox + x, oy + y, 0);
                bool isEdge = island.IsEdgeTile(x, y);
                tilemap.SetTile(pos, isEdge ? (edgeTile ?? cloudTile) : cloudTile);
            }
        }
    }
}
