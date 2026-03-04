using UnityEngine;
using UnityEngine.InputSystem;

public class TileCursor : MonoBehaviour
{
    [SerializeField] private Color validColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.3f);
    [SerializeField] private bool debugLogging = false;

    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;
    private QuickbarUI quickbar;

    // The tile currently highlighted (mouse if valid, else facing)
    public Vector3Int HighlightedTile { get; private set; }
    // True when the cursor is tracking the mouse tile
    public bool IsMouseTargeting { get; private set; }

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = CreateCursorSprite();
        spriteRenderer.sortingLayerName = "GroundDecor";
        spriteRenderer.sortingOrder = 10;

        playerController = FindFirstObjectByType<PlayerController>();
        quickbar = FindFirstObjectByType<QuickbarUI>();

        EventBus.Subscribe<InteriorEnteredEvent>(OnInteriorEntered);
        EventBus.Subscribe<InteriorExitedEvent>(OnInteriorExited);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<InteriorEnteredEvent>(OnInteriorEntered);
        EventBus.Unsubscribe<InteriorExitedEvent>(OnInteriorExited);
    }

    private void OnInteriorEntered(InteriorEnteredEvent evt)
    {
        spriteRenderer.sortingLayerName = InteriorManager.ToInteriorLayer("GroundDecor");
    }

    private void OnInteriorExited(InteriorExitedEvent evt)
    {
        spriteRenderer.sortingLayerName = "GroundDecor";
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

        // Try to target the tile under the mouse cursor
        Vector3Int mouseTile = GetMouseTile();
        bool mouseWalkable = TileManager.Instance != null && TileManager.Instance.IsWalkable(mouseTile);
        bool mouseInRange = IsWithinRange(mouseTile);

        if (debugLogging)
        {
            Vector3Int playerFeetTile = playerController.GetFeetTile();
            Debug.Log($"[TileCursor] mouseTile={mouseTile} walkable={mouseWalkable} inRange={mouseInRange} playerFeetTile={playerFeetTile} feetPos={playerController.GetFeetPosition()}");
        }

        if (mouseWalkable && mouseInRange)
        {
            HighlightedTile = mouseTile;
            IsMouseTargeting = true;
        }
        else
        {
            HighlightedTile = playerController.GetTargetTile();
            IsMouseTargeting = false;
        }

        transform.position = new Vector3(HighlightedTile.x + 0.5f, HighlightedTile.y + 0.5f, 0f);
        spriteRenderer.color = IsValidTargetTile(HighlightedTile) ? validColor : invalidColor;
    }

    private Vector3Int GetMouseTile()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3Int.zero;

        // Use New Input System mouse position
        Vector2 screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Input.mousePosition;

        // For orthographic 2D: pass z = -camera.transform.position.z so world z = 0
        Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPoint);
        worldPos.z = 0f;

        return worldPos.WorldToTile();
    }

    private bool IsWithinRange(Vector3Int tile)
    {
        Vector3Int playerTile = playerController.GetFeetTile();
        int dx = Mathf.Abs(tile.x - playerTile.x);
        int dy = Mathf.Abs(tile.y - playerTile.y);
        int range = GameManager.Instance != null ? GameManager.Instance.PlayerInteractionRange : 1;
        return dx <= range && dy <= range;
    }

    private bool IsValidTargetTile(Vector3Int pos)
    {
        if (TileManager.Instance == null || !TileManager.Instance.IsWalkable(pos))
            return false;

        var slot = quickbar != null ? quickbar.GetActiveSlotData() : null;
        if (slot == null || slot.IsEmpty()) return true;

        var item = slot.item;

        // Data-driven: check tile requirements from the SO
        if (item.tileRequirements != null && item.tileRequirements.Count > 0)
            return TileConditionRegistry.Instance != null
                && TileConditionRegistry.Instance.CheckAll(pos, item.tileRequirements);

        return true;
    }

    private Sprite CreateCursorSprite()
    {
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
