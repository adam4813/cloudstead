using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Cloudstead/Soil Tile")]
public class SoilTile : Tile
{
    [SerializeField] private Sprite tilledSprite;
    [SerializeField] private Sprite wateredSprite;

    private SoilState currentState = SoilState.Tilled;

    public void SetState(SoilState state, Tilemap tilemap, Vector3Int position)
    {
        currentState = state;
        tilemap.RefreshTile(position);
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);

        tileData.sprite = currentState switch
        {
            SoilState.Watered => wateredSprite != null ? wateredSprite : tilledSprite,
            _ => tilledSprite
        };
    }
}