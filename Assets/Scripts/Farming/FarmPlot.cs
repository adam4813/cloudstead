using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    private Vector3Int tilePosition;
    private uint ownerId;
    private SpriteRenderer cropRenderer;

    public SoilState SoilState { get; private set; } = SoilState.Tilled;
    public CropDefinition PlantedCrop { get; private set; }
    public CropStage CurrentStage { get; private set; } = CropStage.Seed;
    public int GrowthProgress { get; private set; }
    public bool IsWatered { get; private set; }

    public void Initialize(Vector3Int pos, uint owner)
    {
        tilePosition = pos;
        ownerId = owner;
        SoilState = SoilState.Tilled;

        // Create crop sprite renderer child
        var cropGO = new GameObject("CropSprite");
        cropGO.transform.SetParent(transform);
        cropGO.transform.localPosition = Vector3.zero;
        cropRenderer = cropGO.AddComponent<SpriteRenderer>();
        cropRenderer.sortingLayerName = "Objects";
        cropRenderer.sortingOrder = 0;
    }

    public void Plant(CropDefinition crop)
    {
        PlantedCrop = crop;
        CurrentStage = CropStage.Seed;
        GrowthProgress = 0;
        UpdateCropSprite();
    }

    public void Water()
    {
        IsWatered = true;
        SoilState = SoilState.Watered;
    }

    public void Grow()
    {
        if (PlantedCrop == null) return;
        if (CurrentStage == CropStage.Mature) return;

        // Out of season — pause growth, don't die
        if (PlantedCrop.growSeasons != null && PlantedCrop.growSeasons.Length > 0)
        {
            var currentSeason = TimeManager.Instance?.CurrentSeason ?? Season.Spring;
            if (System.Array.IndexOf(PlantedCrop.growSeasons, currentSeason) < 0)
                return;
        }

        GrowthProgress++;

        int daysPerStage = Mathf.Max(1, PlantedCrop.growthDays / 3);

        if (GrowthProgress >= PlantedCrop.growthDays)
            CurrentStage = CropStage.Mature;
        else if (GrowthProgress >= daysPerStage * 2)
            CurrentStage = CropStage.Growing;
        else if (GrowthProgress >= daysPerStage)
            CurrentStage = CropStage.Sprout;

        UpdateCropSprite();
    }

    public void ClearCrop()
    {
        PlantedCrop = null;
        CurrentStage = CropStage.Seed;
        GrowthProgress = 0;
        if (cropRenderer != null)
            cropRenderer.sprite = null;
    }

    public void ResetForRegrow()
    {
        CurrentStage = CropStage.Growing;
        GrowthProgress = Mathf.Max(0, PlantedCrop.growthDays - PlantedCrop.regrowDays);
        IsWatered = false;
        UpdateCropSprite();
    }

    public void ResetWatered()
    {
        IsWatered = false;
        SoilState = SoilState.Tilled;
    }

    private void UpdateCropSprite()
    {
        if (cropRenderer == null || PlantedCrop == null) return;

        if (PlantedCrop.stageSprites == null || PlantedCrop.stageSprites.Length == 0)
            return;

        int spriteIndex = CurrentStage switch
        {
            CropStage.Seed => 0,
            CropStage.Sprout => Mathf.Min(1, PlantedCrop.stageSprites.Length - 1),
            CropStage.Growing => Mathf.Min(2, PlantedCrop.stageSprites.Length - 1),
            CropStage.Mature => Mathf.Min(3, PlantedCrop.stageSprites.Length - 1),
            _ => 0
        };

        cropRenderer.sprite = PlantedCrop.stageSprites[spriteIndex];
    }
}
