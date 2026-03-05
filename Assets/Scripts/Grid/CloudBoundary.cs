using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CompositeCollider2D))]
public class CloudBoundary : MonoBehaviour
{
    [SerializeField] private CloudIsland cloudIsland;
    [SerializeField] private AudioClip edgeBumpSound;
    [SerializeField] private float pushBackForce = 8f;

    private AudioSource audioSource;
    private float bumpCooldown;

    private bool _boundaryGenerated;

    private void Start()
    {
        if (cloudIsland == null)
            cloudIsland = GetComponent<CloudIsland>();

        // Configure Rigidbody2D as static so composite works properly
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        var composite = GetComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        if (!_boundaryGenerated)
            GenerateBoundary();
    }

    /// <summary>Destroys existing boundary colliders and regenerates from the current walkability grid.</summary>
    public void RegenerateBoundary()
    {
        // Remove all BoxCollider2D components (they were dynamically added)
        foreach (var col in GetComponents<BoxCollider2D>())
            DestroyImmediate(col);
        GenerateBoundary();

        // Force composite to rebuild
        var composite = GetComponent<CompositeCollider2D>();
        if (composite != null)
            composite.GenerateGeometry();
    }

    private void GenerateBoundary()
    {
        if (cloudIsland == null || cloudIsland.WalkabilityGrid == null)
            return;

        var origin = cloudIsland.TileOrigin;

        for (int x = 0; x < cloudIsland.Width; x++)
        {
            for (int y = 0; y < cloudIsland.Height; y++)
            {
                if (cloudIsland.WalkabilityGrid[x, y]) continue;
                if (!HasWalkableNeighbor(x, y)) continue;

                var col = gameObject.AddComponent<BoxCollider2D>();
                // offset is local space — subtract transform.position from world coords
                col.offset = new Vector2(
                    origin.x + x + 0.5f - transform.position.x,
                    origin.y + y + 0.5f - transform.position.y);
                col.size = Vector2.one;
                col.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
        }

        _boundaryGenerated = true;
    }

    private bool HasWalkableNeighbor(int x, int y)
    {
        var grid = cloudIsland.WalkabilityGrid;
        int w = cloudIsland.Width;
        int h = cloudIsland.Height;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < w && ny >= 0 && ny < h && grid[nx, ny])
                    return true;
            }
        }
        return false;
    }

    public void PlayBumpSound()
    {
        if (edgeBumpSound != null && audioSource != null && bumpCooldown <= 0f)
        {
            audioSource.PlayOneShot(edgeBumpSound, 0.6f);
            bumpCooldown = 0.5f;
        }
    }

    public void PushBack(Rigidbody2D playerRb)
    {
        if (playerRb == null || cloudIsland == null) return;
        Vector2 center = cloudIsland.GetCenterWorldPosition();
        Vector2 pushDir = (center - playerRb.position).normalized;
        playerRb.AddForce(pushDir * pushBackForce);
    }

    private void Update()
    {
        if (bumpCooldown > 0f)
            bumpCooldown -= Time.deltaTime;
    }
}
