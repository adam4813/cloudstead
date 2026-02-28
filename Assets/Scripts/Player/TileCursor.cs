using UnityEngine;

public class TileCursor : MonoBehaviour
{
    [SerializeField] private Color validColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.3f);

    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;

    private void Start()
    {
        // Create a child sprite for the cursor highlight
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = CreateCursorSprite();
        spriteRenderer.sortingLayerName = "GroundDecor";
        spriteRenderer.sortingOrder = 10;

        playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (playerController == null) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.enabled = true;
        Vector3Int targetTile = playerController.GetTargetTile();
        transform.position = new Vector3(targetTile.x + 0.5f, targetTile.y + 0.5f, 0f);

        bool isValid = TileManager.Instance != null
            && TileManager.Instance.IsWalkable(targetTile)
            && IsInteriorTile(targetTile);

        spriteRenderer.color = isValid ? validColor : invalidColor;
    }

    private bool IsInteriorTile(Vector3Int pos)
    {
        var gen = FindFirstObjectByType<CloudGenerator>();
        if (gen == null) return true;

        // Check all 4 cardinal neighbors are also walkable (not an edge)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (!gen.IsWalkable(new Vector3Int(pos.x + dx, pos.y + dy, 0)))
                    return false;
            }
        }
        return true;
    }

    private Sprite CreateCursorSprite()
    {
        // Create a 32x32 border-only texture
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        var transparent = new Color(0, 0, 0, 0);
        var border = Color.white;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                bool isBorder = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                tex.SetPixel(x, y, isBorder ? border : transparent);
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}
