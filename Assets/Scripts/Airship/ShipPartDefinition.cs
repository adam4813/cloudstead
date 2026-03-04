using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

/// <summary>
/// Defines a structural ship part (tile) that can be purchased and placed
/// on the airship during edit mode. These are NOT inventory items — they
/// are bought with gold at the dock and placed directly.
/// </summary>
[System.Serializable]
public class ShipPartDefinition
{
    [Required]
    public string partName;

    [PreviewField(50)]
    public Sprite icon;

    [Required]
    public TileBase tile;

    public AirshipTilemapLayer layer = AirshipTilemapLayer.Floor;

    [Min(0)]
    public int goldCost;
}
