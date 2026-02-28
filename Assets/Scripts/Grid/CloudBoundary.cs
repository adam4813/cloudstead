using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D))]
public class CloudBoundary : MonoBehaviour
{
    [SerializeField] private CloudGenerator cloudGenerator;
    [SerializeField] private float pushBackForce = 8f;

    private void Start()
    {
        if (cloudGenerator == null)
            cloudGenerator = GetComponent<CloudGenerator>();

        GenerateBoundary();
    }

    private void GenerateBoundary()
    {
        if (cloudGenerator == null || cloudGenerator.WalkabilityGrid == null)
            return;

        // Add box colliders on non-walkable tiles adjacent to walkable ones
        for (int x = 0; x < cloudGenerator.Width; x++)
        {
            for (int y = 0; y < cloudGenerator.Height; y++)
            {
                if (cloudGenerator.WalkabilityGrid[x, y]) continue;
                if (!HasWalkableNeighbor(x, y)) continue;

                var col = gameObject.AddComponent<BoxCollider2D>();
                col.offset = new Vector2(x + 0.5f, y + 0.5f);
                col.size = Vector2.one;
                col.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
        }

        // Also add boundary colliders around the grid edges
        AddOuterBoundary();
    }

    private void AddOuterBoundary()
    {
        int w = cloudGenerator.Width;
        int h = cloudGenerator.Height;

        // Bottom and top rows
        for (int x = -1; x <= w; x++)
        {
            AddBoundaryCollider(x, -1);
            AddBoundaryCollider(x, h);
        }
        // Left and right columns
        for (int y = 0; y < h; y++)
        {
            AddBoundaryCollider(-1, y);
            AddBoundaryCollider(w, y);
        }
    }

    private void AddBoundaryCollider(int x, int y)
    {
        var col = gameObject.AddComponent<BoxCollider2D>();
        col.offset = new Vector2(x + 0.5f, y + 0.5f);
        col.size = Vector2.one;
        col.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    private bool HasWalkableNeighbor(int x, int y)
    {
        var grid = cloudGenerator.WalkabilityGrid;
        int w = cloudGenerator.Width;
        int h = cloudGenerator.Height;

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

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Gentle push-back toward cloud center
        Vector3 center = cloudGenerator.GetCenterWorldPosition();
        Vector2 pushDir = ((Vector2)center - collision.transform.position.ToVector2()).normalized;

        var playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb != null)
            playerRb.AddForce(pushDir * pushBackForce);
    }
}
