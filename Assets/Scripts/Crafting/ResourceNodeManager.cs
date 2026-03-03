using System.Collections.Generic;
using UnityEngine;

public class ResourceNodeManager : Singleton<ResourceNodeManager>
{
    private readonly List<CloudGenerator> _clouds = new();
    private readonly Dictionary<ResourceNodeDefinition, int> _activeCounts = new();
    private readonly List<PendingRespawn> _pending = new();
    private System.Random _rng;

    private struct PendingRespawn
    {
        public ResourceNodeDefinition Definition;
        public int DayToSpawn;
    }

    public override void Initialize()
    {
        _rng = new System.Random(Random.Range(0, int.MaxValue));
        EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<DayEndedEvent>(OnDayEnded);
        base.OnDestroy();
    }

    public void RegisterCloud(CloudGenerator cloud)
    {
        if (cloud != null && !_clouds.Contains(cloud))
            _clouds.Add(cloud);
    }

    public void UnregisterCloud(CloudGenerator cloud)
    {
        _clouds.Remove(cloud);
    }

    public void RegisterNode(ResourceNodeDefinition def)
    {
        if (def == null) return;
        if (!_activeCounts.ContainsKey(def)) _activeCounts[def] = 0;
        _activeCounts[def]++;
    }

    public void UnregisterNode(ResourceNodeDefinition def)
    {
        if (def == null) return;
        if (_activeCounts.ContainsKey(def))
            _activeCounts[def] = Mathf.Max(0, _activeCounts[def] - 1);
    }

    public void QueueRespawn(ResourceNodeDefinition def, int currentDay)
    {
        if (def == null) return;
        int days = Random.Range(def.respawnDaysMin, def.respawnDaysMax + 1);
        _pending.Add(new PendingRespawn { Definition = def, DayToSpawn = currentDay + days });
    }

    private void OnDayEnded(DayEndedEvent evt)
    {
        var due = new List<ResourceNodeDefinition>();
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            if (_pending[i].DayToSpawn <= evt.Day)
            {
                due.Add(_pending[i].Definition);
                _pending.RemoveAt(i);
            }
        }
        foreach (var def in due)
            TrySpawnNode(def);
    }

    private void TrySpawnNode(ResourceNodeDefinition def)
    {
        // Find eligible clouds: have this def configured and a prefab assigned
        var eligible = new List<CloudGenerator>();
        int totalMax = 0;
        foreach (var cloud in _clouds)
        {
            if (cloud == null || cloud.NodeConfigs == null) continue;
            foreach (var cfg in cloud.NodeConfigs)
            {
                if (cfg.definition == def && cfg.prefab != null)
                {
                    eligible.Add(cloud);
                    totalMax += cfg.maxCount;
                    break;
                }
            }
        }

        if (eligible.Count == 0) return;

        _activeCounts.TryGetValue(def, out int active);
        if (active >= totalMax) return;

        // Shuffle and try each eligible cloud until one has an open tile
        for (int i = eligible.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (eligible[i], eligible[j]) = (eligible[j], eligible[i]);
        }

        foreach (var cloud in eligible)
        {
            Vector3? pos = cloud.GetRandomObstacleTile(_rng);
            if (!pos.HasValue) continue;

            GameObject prefab = cloud.GetPrefab(def);
            if (prefab == null) continue;

            Transform parent = cloud.ObstacleParent != null ? cloud.ObstacleParent : cloud.transform;
            var go = Instantiate(prefab, pos.Value, Quaternion.identity, parent);
            var node = go.GetComponent<ResourceNode>();
            if (node != null)
                node.Initialize(def);
            return;
        }

        // No tile found — requeue for next day
        _pending.Add(new PendingRespawn
        {
            Definition = def,
            DayToSpawn = (TimeManager.Instance?.CurrentDay ?? 0) + 1
        });
    }
}
