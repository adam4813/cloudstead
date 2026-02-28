#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TileCreator
{
    [MenuItem("Cloudstead/Create Tile Assets From Sprites")]
    public static void CreateTileAssets()
    {
        CreateTile("Assets/Art/Tiles/CloudGround.png", "Assets/Art/Tiles/CloudGround.asset");
        CreateTile("Assets/Art/Tiles/CloudEdge.png", "Assets/Art/Tiles/CloudEdge.asset");
        CreateTile("Assets/Art/Tiles/SkyEmpty.png", "Assets/Art/Tiles/SkyEmpty.asset");
        CreateTile("Assets/Art/Tiles/TilledSoil.png", "Assets/Art/Tiles/TilledSoil.asset");
        CreateTile("Assets/Art/Tiles/WateredSoil.png", "Assets/Art/Tiles/WateredSoil.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TileCreator] All tile assets created in Assets/Art/Tiles/");
    }

    private static void CreateTile(string spritePath, string tilePath)
    {
        // Skip if tile already exists
        if (AssetDatabase.LoadAssetAtPath<Tile>(tilePath) != null)
        {
            Debug.Log($"  Tile already exists: {tilePath}");
            return;
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"  Sprite not found: {spritePath} — import sprites first (Texture Type: Sprite, PPU: 32)");
            return;
        }

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;

        AssetDatabase.CreateAsset(tile, tilePath);
        Debug.Log($"  Created: {tilePath}");
    }
}
#endif
