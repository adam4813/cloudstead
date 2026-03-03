using System.Collections.Generic;
using UnityEngine;

public class ResourceNodeManager : Singleton<ResourceNodeManager>, ISaveable
{
    private readonly List<CloudGenerator> _clouds = new();
    private readonly Dictionary<ResourceNodeDefinition, int> _activeCounts = new();
    private readonly Dictionary<ResourceNodeDefinition, int> _lastSpawnDay = new();
    private System.Random _rng;
    private int _totalDays;

    public override void Initialize()
    {
        _rng = new System.Random(Random.Range(0, int.MaxValue));
        _totalDays = 0;
        EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
        SaveManager.Instance?.Register(this);
    }

    protected override void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
        EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
        base.OnDestroy();
    }

    public void RegisterCloud(CloudGenerator cloud)
    {
        if (cloud == null || _clouds.Contains(cloud)) return;
        _clouds.Add(cloud);
        SpawnInitialNodes(cloud);
    }

    public void UnregisterCloud(CloudGenerator cloud) => _clouds.Remove(cloud);

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

    private void SpawnInitialNodes(CloudGenerator cloud)
    {
        if (cloud.NodeConfigs == null || cloud.NodeConfigs.Length == 0) return;

        var tiles = cloud.GetShuffledCandidateTiles();
        Transform parent = cloud.ObstacleParent != null ? cloud.ObstacleParent : cloud.transform;
        int index = 0;
        int total = 0;

        foreach (var config in cloud.NodeConfigs)
        {
            if (config.prefab == null || config.definition == null) continue;
            int spawned = 0;
            while (spawned < config.maxCount && index < tiles.Count)
            {
                var go = Instantiate(config.prefab, tiles[index++], Quaternion.identity, parent);
                var node = go.GetComponent<ResourceNode>();
                if (node != null) node.Initialize(config.definition);
                spawned++;
                total++;
            }
            // Record spawn time so interval is measured from game start
            if (!_lastSpawnDay.ContainsKey(config.definition))
                _lastSpawnDay[config.definition] = 0;
        }

        Debug.Log($"[ResourceNodeManager] SpawnInitialNodes '{cloud.name}': {total} nodes, {tiles.Count} candidates.");
    }

    private void OnDayStarted(DayStartedEvent evt)
    {
        _totalDays++;

        // Gather all naturally-spawning def types across registered clouds
        var defs = new HashSet<ResourceNodeDefinition>();
        foreach (var cloud in _clouds)
        {
            if (cloud?.NodeConfigs == null) continue;
            foreach (var cfg in cloud.NodeConfigs)
                if (cfg.definition != null && cfg.definition.spawnsNaturally)
                    defs.Add(cfg.definition);
        }

        foreach (var def in defs)
            TryNaturalSpawn(def);
    }

    private void TryNaturalSpawn(ResourceNodeDefinition def)
    {
        _lastSpawnDay.TryGetValue(def, out int lastDay);
        int daysSince = _totalDays - lastDay;

        if (daysSince < def.spawnIntervalMin) return;

        // Probability ramps from 0 at min to 1 at max; guaranteed at or past max
        int amount;
        if (daysSince >= def.spawnIntervalMax || def.spawnIntervalMax <= def.spawnIntervalMin)
        {
            amount = Random.Range(def.spawnCountMin, def.spawnCountMax + 1);
        }
        else
        {
            float t = (float)(daysSince - def.spawnIntervalMin) / (def.spawnIntervalMax - def.spawnIntervalMin);
            if (Random.value > t) return;
            amount = Random.Range(def.spawnCountMin, def.spawnCountMax + 1);
        }

        // Find eligible clouds and total cap
        var eligible = new List<CloudGenerator>();
        int totalMax = 0;
        foreach (var cloud in _clouds)
        {
            if (cloud?.NodeConfigs == null) continue;
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
        int canSpawn = Mathf.Min(amount, totalMax - active);
        if (canSpawn <= 0) return;

        // Shuffle clouds then place nodes
        for (int i = eligible.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (eligible[i], eligible[j]) = (eligible[j], eligible[i]);
        }

        int spawned = 0;
        foreach (var cloud in eligible)
        {
            while (spawned < canSpawn)
            {
                Vector3? pos = cloud.GetRandomObstacleTile(_rng);
                if (!pos.HasValue) break;
                GameObject prefab = cloud.GetPrefab(def);
                if (prefab == null) break;
                Transform parent = cloud.ObstacleParent != null ? cloud.ObstacleParent : cloud.transform;
                var go = Instantiate(prefab, pos.Value, Quaternion.identity, parent);
                var node = go.GetComponent<ResourceNode>();
                if (node != null) node.Initialize(def);
                spawned++;
            }
            if (spawned >= canSpawn) break;
        }

        if (spawned > 0)
        {
            _lastSpawnDay[def] = _totalDays;
            Debug.Log($"[ResourceNodeManager] Natural spawn: {spawned}x {def.nodeName} on day {_totalDays}.");
        }
    }

    #region ISaveable

    public string SaveState()
    {
        var existing = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        var nodeEntries = new List<NodeSaveEntry>();
        foreach (var node in existing)
        {
            if (node == null || node.Definition == null) continue;
            var pos = node.transform.position;
            nodeEntries.Add(new NodeSaveEntry
            {
                definitionName = node.Definition.nodeName,
                posX = pos.x,
                posY = pos.y,
                currentHits = node.CurrentHits
            });
        }

        var spawnEntries = new List<SpawnDayEntry>();
        foreach (var kvp in _lastSpawnDay)
        {
            if (kvp.Key == null) continue;
            spawnEntries.Add(new SpawnDayEntry
            {
                definitionName = kvp.Key.nodeName,
                day = kvp.Value
            });
        }

        var data = new NodeManagerSaveData
        {
            totalDays = _totalDays,
            nodes = nodeEntries.ToArray(),
            lastSpawnDays = spawnEntries.ToArray()
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<NodeManagerSaveData>(json);
        _totalDays = data.totalDays;

        // Rebuild _lastSpawnDay
        if (data.lastSpawnDays != null)
        {
            foreach (var entry in data.lastSpawnDays)
            {
                var def = FindDefinitionByName(entry.definitionName);
                if (def != null)
                    _lastSpawnDay[def] = entry.day;
            }
        }

        // Destroy all existing resource nodes
        var existing = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        foreach (var node in existing)
            Destroy(node.gameObject);

        // Clear active counts — they'll be re-registered by new nodes in Start()
        _activeCounts.Clear();

        // Recreate nodes from saved data
        if (data.nodes != null)
        {
            foreach (var entry in data.nodes)
            {
                var def = FindDefinitionByName(entry.definitionName);
                if (def == null) continue;

                GameObject prefab = null;
                Transform parent = null;
                foreach (var cloud in _clouds)
                {
                    prefab = cloud.GetPrefab(def);
                    if (prefab != null)
                    {
                        parent = cloud.ObstacleParent != null ? cloud.ObstacleParent : cloud.transform;
                        break;
                    }
                }
                if (prefab == null) continue;

                var pos = new Vector3(entry.posX, entry.posY, 0f);
                var go = Instantiate(prefab, pos, Quaternion.identity, parent);
                var node = go.GetComponent<ResourceNode>();
                if (node != null)
                {
                    node.Initialize(def);
                    node.Restore(entry.currentHits);
                }
            }
        }
    }

    private ResourceNodeDefinition FindDefinitionByName(string defName)
    {
        foreach (var cloud in _clouds)
        {
            if (cloud?.NodeConfigs == null) continue;
            foreach (var cfg in cloud.NodeConfigs)
            {
                if (cfg.definition != null && cfg.definition.nodeName == defName)
                    return cfg.definition;
            }
        }
        return null;
    }

    [System.Serializable]
    private class NodeManagerSaveData
    {
        public int totalDays;
        public NodeSaveEntry[] nodes;
        public SpawnDayEntry[] lastSpawnDays;
    }

    [System.Serializable]
    private class NodeSaveEntry
    {
        public string definitionName;
        public float posX, posY;
        public int currentHits;
    }

    [System.Serializable]
    private class SpawnDayEntry
    {
        public string definitionName;
        public int day;
    }

    #endregion
}