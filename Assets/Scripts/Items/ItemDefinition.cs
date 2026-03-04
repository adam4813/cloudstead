using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewItem", menuName = "Cloudstead/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [FoldoutGroup("Basic Info")]
    [Required]
    public string itemName;

    [FoldoutGroup("Basic Info")]
    [TextArea(2, 4)]
    public string description;

    [FoldoutGroup("Basic Info")]
    [PreviewField(64)]
    [Required]
    public Sprite icon;

    [FoldoutGroup("Basic Info")]
    public ItemCategory category;

    [FoldoutGroup("Stacking")]
    public int maxStack = 99;

    [FoldoutGroup("Economy")]
    [Min(0)]
    public int buyPrice;

    [FoldoutGroup("Economy")]
    [Min(0)]
    public int sellPrice;

    [FoldoutGroup("Placement")]
    public bool isPlaceable;

    [FoldoutGroup("Placement")]
    [Tooltip("Optional prefab to spawn when placed. If null, creates a default PlacedItem with the item icon.")]
    public GameObject placeablePrefab;

    [FoldoutGroup("Placement")]
    [Tooltip("Zones where this item can be placed")]
    public PlacementZone allowedZones = PlacementZone.All;

    [FoldoutGroup("Usage Rules")]
    [Tooltip("Tile conditions that must ALL pass for this item to be used/placed/planted at a tile")]
    public List<TileConditionTag> tileRequirements = new();

    [FoldoutGroup("Airship")]
    [Tooltip("If set, this item places a tile on the airship's tilemap instead of spawning a PlacedItem")]
    public UnityEngine.Tilemaps.TileBase airshipTile;

    [FoldoutGroup("Airship")]
    [Tooltip("The tilemap layer this ship part targets: Floor, Decoration, Walls")]
    public AirshipTilemapLayer airshipLayer = AirshipTilemapLayer.Floor;

    [FoldoutGroup("Consumable")]
    [Tooltip("Amount of stamina restored when this item is eaten. 0 = not a consumable.")]
    [Min(0)]
    public int staminaRestore;

    /// <summary>
    /// Unique identifier for save/load. Falls back to asset name.
    /// </summary>
    public string ItemId => string.IsNullOrEmpty(itemName) ? name : itemName;
}
