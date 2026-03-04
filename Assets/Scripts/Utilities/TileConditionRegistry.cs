using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Maps TileConditionTag assets to tile-validation functions.
/// Built-in tags are assigned via serialized fields in the inspector.
/// Mods can call RegisterCondition() to add custom evaluators at runtime.
/// </summary>
public class TileConditionRegistry : Singleton<TileConditionRegistry>
{
    [FoldoutGroup("Built-in Conditions")]
    [Required] [SerializeField] private TileConditionTag walkable;
    [FoldoutGroup("Built-in Conditions")]
    [Required] [SerializeField] private TileConditionTag nonEdge;
    [FoldoutGroup("Built-in Conditions")]
    [Required] [SerializeField] private TileConditionTag hasFarmPlot;
    [FoldoutGroup("Built-in Conditions")]
    [Required] [SerializeField] private TileConditionTag noFarmPlot;
    [FoldoutGroup("Built-in Conditions")]
    [Required] [SerializeField] private TileConditionTag emptyFarmPlot;
    [FoldoutGroup("Built-in Conditions")]
    [Required] [SerializeField] private TileConditionTag hasMatureCrop;

    private readonly Dictionary<TileConditionTag, Func<Vector3Int, bool>> _checks = new();

    public override void Initialize()
    {
        RegisterBuiltInConditions();
    }

    /// <summary>Register a custom condition evaluator for a tag. Mods use this.</summary>
    public void RegisterCondition(TileConditionTag tag, Func<Vector3Int, bool> check)
    {
        if (tag == null || check == null) return;
        _checks[tag] = check;
    }

    /// <summary>Returns true only if every tag in the list passes for the given position.</summary>
    public bool CheckAll(Vector3Int pos, List<TileConditionTag> conditions)
    {
        if (conditions == null || conditions.Count == 0) return true;

        foreach (var tag in conditions)
        {
            if (tag == null) continue;
            if (_checks.TryGetValue(tag, out var check))
            {
                if (!check(pos)) return false;
            }
            else
            {
                Debug.LogWarning($"[TileConditionRegistry] No evaluator registered for '{tag.displayName}'");
            }
        }
        return true;
    }

    private void RegisterBuiltInConditions()
    {
        RegisterCondition(walkable, pos =>
            TileManager.Instance != null && TileManager.Instance.IsWalkable(pos));

        RegisterCondition(nonEdge, pos =>
            TileManager.Instance != null && !TileManager.Instance.IsEdgeTile(pos));

        RegisterCondition(hasFarmPlot, pos =>
            FarmingManager.Instance != null && FarmingManager.Instance.HasPlotAt(pos));

        RegisterCondition(noFarmPlot, pos =>
            FarmingManager.Instance == null || !FarmingManager.Instance.HasPlotAt(pos));

        RegisterCondition(emptyFarmPlot, pos =>
        {
            if (FarmingManager.Instance == null) return false;
            var plot = FarmingManager.Instance.GetPlotAt(pos);
            return plot != null && plot.PlantedCrop == null;
        });

        RegisterCondition(hasMatureCrop, pos =>
        {
            if (FarmingManager.Instance == null) return false;
            var plot = FarmingManager.Instance.GetPlotAt(pos);
            return plot != null && plot.PlantedCrop != null
                   && plot.CurrentStage == CropStage.Mature;
        });
    }
}

