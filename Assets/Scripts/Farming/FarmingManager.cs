using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class FarmingManager : Singleton<FarmingManager>
{
    [SerializeField] private TileBase tilledSoilTile;
    [SerializeField] private TileBase wateredSoilTile;
    [SerializeField] private GameObject cropPrefab;

    private Dictionary<Vector3Int, FarmPlot> farmPlots = new();

    public override void Initialize()
    {
        EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
        base.OnDestroy();
    }

    public bool TillSoil(Vector3Int pos, uint playerId)
    {
        if (TileManager.Instance == null) return false;
        if (!TileManager.Instance.IsWalkable(pos)) return false;
        if (!IsInteriorTile(pos)) return false;
        if (farmPlots.ContainsKey(pos)) return false;

        TileManager.Instance.SetTile(pos, tilledSoilTile, TileManager.Instance.SoilTilemap);

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
            TileManager.Instance.SetTile(pos, wateredSoilTile, TileManager.Instance.SoilTilemap);
            return true;
        }

        if (plot.PlantedCrop == null) return false;

        plot.Water();
        TileManager.Instance.SetTile(pos, wateredSoilTile, TileManager.Instance.SoilTilemap);

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
            TileManager.Instance.SetTile(pos, tilledSoilTile, TileManager.Instance.SoilTilemap);
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
                TileManager.Instance.SetTile(pos, tilledSoilTile, TileManager.Instance.SoilTilemap);
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

    private bool IsInteriorTile(Vector3Int pos)
    {
        var gen = FindFirstObjectByType<CloudGenerator>();
        if (gen == null) return true;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (!gen.IsWalkable(new Vector3Int(pos.x + dx, pos.y + dy, 0)))
                    return false;
            }
        }
        return true;
    }
}
