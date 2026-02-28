using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewCrop", menuName = "Cloudstead/Items/Crop Definition")]
public class CropDefinition : ItemDefinition
{
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
    [Required]
    public ItemDefinition harvestItem;

    [FoldoutGroup("Harvest")]
    [Min(1)]
    public int harvestYield = 1;

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
            CropStage.Wilted => 3, // Show mature sprite but tinted by renderer
            _ => 0
        };
        return (stageSprites != null && index < stageSprites.Length) ? stageSprites[index] : null;
    }
}
