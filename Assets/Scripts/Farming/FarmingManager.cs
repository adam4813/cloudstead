using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class FarmingManager : Singleton<FarmingManager>, ISaveable
{
    [SerializeField] private TileBase tilledSoilTile;
    [SerializeField] private TileBase wateredSoilTile;
    [SerializeField] private GameObject cropPrefab;

    private Dictionary<Vector3Int, FarmPlot> farmPlots = new();

    public override void Initialize()
    {
        EventBus.Subscribe<DayStartedEvent>(OnDayStarted);

        if (SaveManager.Instance != null)
            SaveManager.Instance.Register(this);
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
        base.OnDestroy();
    }

    public bool TillSoil(Vector3Int pos, uint playerId)
    {
        if (CloudIsland.Current == null) return false;
        if (!CloudIsland.IsCurrentWalkable(pos)) return false;
        if (farmPlots.ContainsKey(pos)) return false;

        CloudIsland.Current.SoilTilemap?.SetTile(pos, tilledSoilTile);

        var plotGO = new GameObject($"FarmPlot_{pos.x}_{pos.y}");
        plotGO.transform.position = pos.TileToWorld();
        var plot = plotGO.AddComponent<FarmPlot>();
        plot.Initialize(pos, playerId);
        farmPlots[pos] = plot;

        return true;
    }

    public bool PlantSeed(Vector3Int pos, CropDefinition crop, uint playerId)
    {
        if (!farmPlots.TryGetValue(pos, out var plot)) return false;
        if (plot.CurrentStage != CropStage.Seed || plot.PlantedCrop != null) return false;

        // Enforce seasonal restrictions (cozy: refuse planting, not dying)
        if (crop.growSeasons != null && crop.growSeasons.Length > 0)
        {
            var currentSeason = TimeManager.Instance?.CurrentSeason ?? Season.Spring;
            bool inSeason = System.Array.IndexOf(crop.growSeasons, currentSeason) >= 0;
            if (!inSeason) return false;
        }

        plot.Plant(crop);

        EventBus.Publish(new CropPlantedEvent { Crop = crop, Position = pos });
        return true;
    }

    public bool WaterPlot(Vector3Int pos, uint playerId)
    {
        if (!farmPlots.TryGetValue(pos, out var plot)) return false;
        if (plot.PlantedCrop == null && plot.SoilState == SoilState.Tilled)
        {
            plot.Water();
            CloudIsland.Current?.SoilTilemap?.SetTile(pos, wateredSoilTile);
            return true;
        }

        if (plot.PlantedCrop == null) return false;

        plot.Water();
        CloudIsland.Current?.SoilTilemap?.SetTile(pos, wateredSoilTile);

        EventBus.Publish(new CropWateredEvent { Position = pos });
        return true;
    }

    public bool HarvestCrop(Vector3Int pos, uint playerId, out HarvestOutput[] outputs)
    {
        outputs = null;

        if (!farmPlots.TryGetValue(pos, out var plot)) return false;
        if (plot.PlantedCrop == null) return false;
        if (plot.CurrentStage != CropStage.Mature) return false;

        outputs = plot.PlantedCrop.harvestOutputs;

        EventBus.Publish(new CropHarvestedEvent { Crop = plot.PlantedCrop });

        // Reset plot
        if (plot.PlantedCrop.regrowDays > 0)
        {
            plot.ResetForRegrow();
        }
        else
        {
            plot.ClearCrop();
            CloudIsland.Current?.SoilTilemap?.SetTile(pos, tilledSoilTile);
        }

        return true;
    }

    private void OnDayStarted(DayStartedEvent evt)
    {
        AdvanceDay();
    }

    public void AdvanceDay()
    {
        foreach (var kvp in farmPlots)
        {
            var plot = kvp.Value;
            var pos = kvp.Key;

            // Grow planted, watered crops
            if (plot.PlantedCrop != null && plot.IsWatered)
            {
                plot.Grow();
                if (plot.CurrentStage != CropStage.Mature)
                {
                    EventBus.Publish(new CropGrownEvent
                    {
                        Crop = plot.PlantedCrop,
                        NewStage = plot.CurrentStage
                    });
                }
            }

            // All watered plots (planted or empty) dry out overnight
            if (plot.IsWatered)
            {
                plot.ResetWatered();
                CloudIsland.Current?.SoilTilemap?.SetTile(pos, tilledSoilTile);
            }
        }
    }

    public FarmPlot GetPlotAt(Vector3Int pos)
    {
        farmPlots.TryGetValue(pos, out var plot);
        return plot;
    }

    public bool HasPlotAt(Vector3Int pos)
    {
        return farmPlots.ContainsKey(pos);
    }

    public string SaveState()
    {
        var plotList = new List<FarmPlotSaveData>();
        foreach (var kvp in farmPlots)
        {
            var pos = kvp.Key;
            var plot = kvp.Value;
            plotList.Add(new FarmPlotSaveData
            {
                posX = pos.x,
                posY = pos.y,
                posZ = pos.z,
                cropName = plot.PlantedCrop != null ? plot.PlantedCrop.cropName : "",
                growthProgress = plot.GrowthProgress,
                soilState = (int)plot.SoilState,
                cropStage = (int)plot.CurrentStage,
                isWatered = plot.IsWatered
            });
        }
        return JsonUtility.ToJson(new FarmingSaveData { plots = plotList.ToArray() });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<FarmingSaveData>(json);
        if (data?.plots == null) return;

        foreach (var kvp in farmPlots)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        farmPlots.Clear();

        var db = GameBootstrapper.Database;
        if (db == null) { Debug.LogError("[FarmingManager] GameDatabase not assigned"); return; }

        foreach (var plotData in data.plots)
        {
            var pos = new Vector3Int(plotData.posX, plotData.posY, plotData.posZ);

            if (CloudIsland.Current != null)
            {
                var tile = plotData.isWatered ? wateredSoilTile : tilledSoilTile;
                CloudIsland.Current.SoilTilemap?.SetTile(pos, tile);
            }

            var plotGO = new GameObject($"FarmPlot_{pos.x}_{pos.y}");
            plotGO.transform.position = pos.TileToWorld();
            var plot = plotGO.AddComponent<FarmPlot>();
            plot.Initialize(pos, 0);

            CropDefinition crop = null;
            if (!string.IsNullOrEmpty(plotData.cropName))
                crop = db.GetCrop(plotData.cropName);

            plot.Restore(crop, plotData.growthProgress, (CropStage)plotData.cropStage,
                (SoilState)plotData.soilState, plotData.isWatered);

            farmPlots[pos] = plot;
        }
    }

    [System.Serializable]
    private class FarmingSaveData
    {
        public FarmPlotSaveData[] plots;
    }

    [System.Serializable]
    private class FarmPlotSaveData
    {
        public int posX;
        public int posY;
        public int posZ;
        public string cropName;
        public int growthProgress;
        public int soilState;
        public int cropStage;
        public bool isWatered;
    }
}
