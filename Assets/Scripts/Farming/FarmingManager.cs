using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class FarmingManager : Singleton<FarmingManager>, ISaveable
{
    [SerializeField] private TileBase tilledSoilTile;
    [SerializeField] private TileBase wateredSoilTile;
    [SerializeField] private GameObject cropPrefab;

    // Plots grouped by context (cloud, interior, airship)
    private readonly Dictionary<string, Dictionary<Vector3Int, FarmPlot>> _plotsByContext = new();

    // Registered contexts for AdvanceDay tile visual updates
    private readonly Dictionary<string, IFarmingContext> _contextRegistry = new();

    private PlayerController _playerController;

    public override void Initialize()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerController = player.GetComponent<PlayerController>();

        EventBus.Subscribe<DayStartedEvent>(OnDayStarted);

        if (SaveManager.Instance != null)
            SaveManager.Instance.Register(this);
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
        base.OnDestroy();
    }

    // ── Context Resolution ──────────────────────────────────────────────────

    /// <summary>
    /// Resolves the active IFarmingContext: interior → airship → cloud, in priority order.
    /// Returns null if no context is available.
    /// </summary>
    public IFarmingContext GetCurrentContext()
    {
        if (InteriorManager.Instance != null && InteriorManager.Instance.IsInsideInterior)
            return InteriorManager.Instance.CurrentInterior;

        if (_playerController != null && _playerController.CurrentAirship != null)
            return _playerController.CurrentAirship;

        return CloudIsland.Current;
    }

    private Dictionary<Vector3Int, FarmPlot> GetOrCreatePlots(IFarmingContext ctx)
    {
        if (!_plotsByContext.TryGetValue(ctx.ContextId, out var plots))
        {
            plots = new Dictionary<Vector3Int, FarmPlot>();
            _plotsByContext[ctx.ContextId] = plots;
        }
        _contextRegistry[ctx.ContextId] = ctx;
        return plots;
    }

    private Dictionary<Vector3Int, FarmPlot> GetCurrentPlots()
    {
        var ctx = GetCurrentContext();
        if (ctx == null) return null;
        return _plotsByContext.TryGetValue(ctx.ContextId, out var plots) ? plots : null;
    }

    // ── Farming Operations ──────────────────────────────────────────────────

    public bool TillSoil(Vector3Int pos, uint playerId)
    {
        var ctx = GetCurrentContext();
        if (ctx?.SoilTilemap == null) return false;
        if (!ctx.IsWalkable(pos)) return false;

        var plots = GetOrCreatePlots(ctx);
        if (plots.ContainsKey(pos)) return false;

        ctx.SoilTilemap.SetTile(pos, tilledSoilTile);

        var plotGO = new GameObject($"FarmPlot_{pos.x}_{pos.y}");
        plotGO.transform.position = pos.TileToWorld();
        var plot = plotGO.AddComponent<FarmPlot>();
        plot.Initialize(pos, playerId, ctx.ContextId);
        plots[pos] = plot;

        return true;
    }

    public bool PlantSeed(Vector3Int pos, CropDefinition crop, uint playerId)
    {
        var plots = GetCurrentPlots();
        if (plots == null || !plots.TryGetValue(pos, out var plot)) return false;
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
        var ctx = GetCurrentContext();
        if (ctx == null) return false;
        var plots = GetCurrentPlots();
        if (plots == null || !plots.TryGetValue(pos, out var plot)) return false;

        if (plot.PlantedCrop == null && plot.SoilState == SoilState.Tilled)
        {
            plot.Water();
            ctx.SoilTilemap?.SetTile(pos, wateredSoilTile);
            return true;
        }

        if (plot.PlantedCrop == null) return false;

        plot.Water();
        ctx.SoilTilemap?.SetTile(pos, wateredSoilTile);

        EventBus.Publish(new CropWateredEvent { Position = pos });
        return true;
    }

    public bool HarvestCrop(Vector3Int pos, uint playerId, out HarvestOutput[] outputs)
    {
        outputs = null;

        var ctx = GetCurrentContext();
        if (ctx == null) return false;
        var plots = GetCurrentPlots();
        if (plots == null || !plots.TryGetValue(pos, out var plot)) return false;
        if (plot.PlantedCrop == null) return false;
        if (plot.CurrentStage != CropStage.Mature) return false;

        outputs = plot.PlantedCrop.harvestOutputs;

        EventBus.Publish(new CropHarvestedEvent { Crop = plot.PlantedCrop });

        if (plot.PlantedCrop.regrowDays > 0)
        {
            plot.ResetForRegrow();
        }
        else
        {
            plot.ClearCrop();
            ctx.SoilTilemap?.SetTile(pos, tilledSoilTile);
        }

        return true;
    }

    // ── Day Cycle ───────────────────────────────────────────────────────────

    private void OnDayStarted(DayStartedEvent evt)
    {
        AdvanceDay();
    }

    public void AdvanceDay()
    {
        foreach (var (contextId, plots) in _plotsByContext)
        {
            _contextRegistry.TryGetValue(contextId, out var ctx);
            // Context may have been destroyed (scene unload); tile visuals are skipped gracefully
            var soilTilemap = (ctx as Object) != null ? ctx.SoilTilemap : null;

            foreach (var (pos, plot) in plots)
            {
                if (plot == null) continue;

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

                if (plot.IsWatered)
                {
                    plot.ResetWatered();
                    soilTilemap?.SetTile(pos, tilledSoilTile);
                }
            }
        }
    }

    // ── Queries ─────────────────────────────────────────────────────────────

    public FarmPlot GetPlotAt(Vector3Int pos)
    {
        var plots = GetCurrentPlots();
        if (plots != null && plots.TryGetValue(pos, out var plot)) return plot;
        return null;
    }

    public bool HasPlotAt(Vector3Int pos)
    {
        var plots = GetCurrentPlots();
        return plots != null && plots.ContainsKey(pos);
    }

    // ── Save / Load ─────────────────────────────────────────────────────────

    public string SaveState()
    {
        var plotList = new List<FarmPlotSaveData>();
        foreach (var (contextId, plots) in _plotsByContext)
        {
            foreach (var (pos, plot) in plots)
            {
                if (plot == null) continue;
                plotList.Add(new FarmPlotSaveData
                {
                    contextId = contextId,
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
        }
        return JsonUtility.ToJson(new FarmingSaveData { plots = plotList.ToArray() });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<FarmingSaveData>(json);
        if (data?.plots == null) return;

        foreach (var (_, plots) in _plotsByContext)
        {
            foreach (var (_, plot) in plots)
                if (plot != null) Destroy(plot.gameObject);
        }
        _plotsByContext.Clear();

        var db = GameBootstrapper.Database;
        if (db == null) { Debug.LogError("[FarmingManager] GameDatabase not assigned"); return; }

        foreach (var plotData in data.plots)
        {
            var pos = new Vector3Int(plotData.posX, plotData.posY, plotData.posZ);
            string ctxId = string.IsNullOrEmpty(plotData.contextId) ? FallbackContextId() : plotData.contextId;

            // Resolve context for tile visuals (may be null if not loaded yet)
            if (_contextRegistry.TryGetValue(ctxId, out var ctx) && ctx?.SoilTilemap != null)
            {
                var tile = plotData.isWatered ? wateredSoilTile : tilledSoilTile;
                ctx.SoilTilemap.SetTile(pos, tile);
            }

            if (!_plotsByContext.TryGetValue(ctxId, out var plots))
            {
                plots = new Dictionary<Vector3Int, FarmPlot>();
                _plotsByContext[ctxId] = plots;
            }

            var plotGO = new GameObject($"FarmPlot_{pos.x}_{pos.y}");
            plotGO.transform.position = pos.TileToWorld();
            var plot = plotGO.AddComponent<FarmPlot>();
            plot.Initialize(pos, 0, ctxId);

            CropDefinition crop = null;
            if (!string.IsNullOrEmpty(plotData.cropName))
                crop = db.GetCrop(plotData.cropName);

            plot.Restore(crop, plotData.growthProgress, (CropStage)plotData.cropStage,
                (SoilState)plotData.soilState, plotData.isWatered);

            plots[pos] = plot;
        }
    }

    /// <summary>Fallback for save data that predates context-scoped plots.</summary>
    private static string FallbackContextId()
    {
        return CloudIsland.Current != null ? $"cloud:{CloudIsland.Current.CloudId}" : "cloud:unknown";
    }

    [System.Serializable]
    private class FarmingSaveData
    {
        public FarmPlotSaveData[] plots;
    }

    [System.Serializable]
    private class FarmPlotSaveData
    {
        public string contextId;
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
