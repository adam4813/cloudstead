using UnityEngine;

/// <summary>
/// Debug overlay that shows the tile the player's feet are currently occupying.
/// Attach to the Player GameObject. Disable or destroy in production builds.
/// </summary>
public class PlayerTileDebug : MonoBehaviour
{
    [SerializeField] private Color tileColor = new Color(0f, 1f, 1f, 0.35f);

    private SpriteRenderer indicator;

    private void Start()
    {
        var go = new GameObject("_PlayerTileDebug");
        go.transform.SetParent(null); // world space, not child of player

        indicator = go.AddComponent<SpriteRenderer>();
        indicator.sprite = CreateSquareSprite();
        indicator.sortingLayerName = "GroundDecor";
        indicator.sortingOrder = 9; // below TileCursor (10)
        indicator.color = tileColor;
    }

    private void LateUpdate()
    {
        if (indicator == null) return;

        // Use feet tile so the debug square matches the player's ground position
        PlayerController pc = GetComponent<PlayerController>();
        Vector3Int tile = pc != null ? pc.GetFeetTile() : transform.position.WorldToTile();
        indicator.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
    }

    private void OnDestroy()
    {
        if (indicator != null)
            Destroy(indicator.gameObject);
    }

    private Sprite CreateSquareSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // Solid fill
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}
