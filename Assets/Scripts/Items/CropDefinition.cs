using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewCrop", menuName = "Cloudstead/Crops/Crop Definition")]
public class CropDefinition : ScriptableObject
{
    [FoldoutGroup("Basic Info")]
    [Required]
    public string cropName;

    [FoldoutGroup("Basic Info")]
    [TextArea(2, 4)]
    public string description;

    [FoldoutGroup("Basic Info")]
    [PreviewField(64)]
    public Sprite icon;

    [FoldoutGroup("Growth")]
    [Required]
    [Tooltip("The seed item consumed when planting")]
    public ItemDefinition seedItem;

    [FoldoutGroup("Growth")]
    [Min(1)]
    public int growthDays = 3;

    [FoldoutGroup("Growth")]
    public Season[] growSeasons;

    [FoldoutGroup("Growth")]
    [PreviewField(48)]
    [Tooltip("Sprites for each growth stage: Seed, Sprout, Growing, Mature")]
    public Sprite[] stageSprites = new Sprite[4];

    [FoldoutGroup("Harvest")]
    [Tooltip("Items produced when harvested (e.g. Sunflower + Sunflower Seeds)")]
    public HarvestOutput[] harvestOutputs = new HarvestOutput[1];

    [FoldoutGroup("Harvest")]
    [Tooltip("-1 means no regrowth")]
    public int regrowDays = -1;

    public Sprite GetStageSprite(CropStage stage)
    {
        int index = stage switch
        {
            CropStage.Seed => 0,
            CropStage.Sprout => 1,
            CropStage.Growing => 2,
            CropStage.Mature => 3,
            _ => 0
        };
        return (stageSprites != null && index < stageSprites.Length) ? stageSprites[index] : null;
    }
}

[System.Serializable]
public class HarvestOutput
{
    [Required]
    public ItemDefinition item;
    [Min(1)]
    public int minYield = 1;
    [Min(1)]
    public int maxYield = 1;
}
