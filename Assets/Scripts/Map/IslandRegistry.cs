using System.Collections.Generic;
using UnityEngine;

public class IslandRegistry : Singleton<IslandRegistry>, ISaveable
{
    private readonly List<IslandInfo> _islands = new();

    public IReadOnlyList<IslandInfo> Islands => _islands;

    public override void Initialize()
    {
        SaveManager.Instance?.Register(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SaveManager.Instance?.Unregister(this);
    }

    public void RegisterIsland(IslandInfo info)
    {
        // Update if already registered (e.g. after regeneration)
        for (int i = 0; i < _islands.Count; i++)
        {
            if (_islands[i].cloudId == info.cloudId)
            {
                _islands[i] = info;
                return;
            }
        }

        _islands.Add(info);
        EventBus.Publish(new IslandDiscoveredEvent { CloudId = info.cloudId, DisplayName = info.displayName });
    }

    public IslandInfo? GetHome()
    {
        foreach (var island in _islands)
            if (island.isHome) return island;
        return null;
    }

    #region ISaveable

    public string SaveState()
    {
        return JsonUtility.ToJson(new IslandRegistrySaveData { islands = _islands.ToArray() });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<IslandRegistrySaveData>(json);
        if (data?.islands == null) return;
        _islands.Clear();
        _islands.AddRange(data.islands);
    }

    [System.Serializable]
    private class IslandRegistrySaveData
    {
        public IslandInfo[] islands;
    }

    #endregion
}
