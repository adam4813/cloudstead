using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public struct SaveSlotMetadata
{
    public bool exists;
    public int day;
    public string season;
    public int year;
}

public class SaveManager : Singleton<SaveManager>
{
    [SerializeField] private string activeSlotName = "slot1";

    public string ActiveSlot => activeSlotName;

    private readonly List<ISaveable> saveables = new();

    public void Register(ISaveable saveable)
    {
        if (!saveables.Contains(saveable))
            saveables.Add(saveable);
    }

    public void Unregister(ISaveable saveable)
    {
        saveables.Remove(saveable);
    }

    public void Save(string slotName)
    {
        var saveData = new Dictionary<string, string>();
        foreach (var saveable in saveables)
        {
            string key = saveable.GetType().Name;
            saveData[key] = saveable.SaveState();
        }

        string json = JsonUtility.ToJson(new SaveWrapper(saveData), true);
        string path = GetSavePath(slotName);
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, json);
        Debug.Log($"[SaveManager] Saved to {path}");
    }

    public void Load(string slotName)
    {
        string path = GetSavePath(slotName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveManager] No save found at {path}");
            return;
        }

        string json = File.ReadAllText(path);
        var saveData = JsonUtility.FromJson<SaveWrapper>(json);

        foreach (var saveable in saveables)
        {
            string key = saveable.GetType().Name;
            if (saveData.TryGet(key, out string data))
                saveable.RestoreState(data);
        }

        Debug.Log($"[SaveManager] Loaded from {path}");
    }

    public void DeleteSlot(string slotName)
    {
        string path = GetSavePath(slotName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public bool SlotExists(string slotName)
    {
        return File.Exists(GetSavePath(slotName));
    }

    public void SetActiveSlot(string slotName)
    {
        activeSlotName = slotName;
    }

    public string[] GetAllSlotNames()
    {
        return new[] { "slot1", "slot2", "slot3" };
    }

    public SaveSlotMetadata GetSlotMetadata(string slotName)
    {
        string path = GetSavePath(slotName);
        if (!File.Exists(path))
            return new SaveSlotMetadata { exists = false };

        try
        {
            string json = File.ReadAllText(path);
            var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
            if (wrapper != null && wrapper.TryGet("TimeManager", out string timeJson))
            {
                var timeData = JsonUtility.FromJson<TimeMetadata>(timeJson);
                return new SaveSlotMetadata
                {
                    exists = true,
                    day = timeData.currentDay,
                    season = ((Season)timeData.currentSeason).ToString(),
                    year = timeData.currentYear
                };
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveManager] Failed to read metadata for {slotName}: {e.Message}");
        }

        return new SaveSlotMetadata { exists = true, day = 0, season = "Unknown", year = 0 };
    }

    private string GetSavePath(string slotName)
    {
        return Path.Combine(Application.persistentDataPath, "saves", $"{slotName}.json");
    }

    [System.Serializable]
    private class SaveWrapper
    {
        public string[] keys;
        public string[] values;

        public SaveWrapper() { }

        public SaveWrapper(Dictionary<string, string> data)
        {
            keys = new string[data.Count];
            values = new string[data.Count];
            int i = 0;
            foreach (var kvp in data)
            {
                keys[i] = kvp.Key;
                values[i] = kvp.Value;
                i++;
            }
        }

        public bool TryGet(string key, out string value)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == key)
                {
                    value = values[i];
                    return true;
                }
            }
            value = null;
            return false;
        }
    }

    [System.Serializable]
    private class TimeMetadata
    {
        public float currentTime;
        public int currentDay;
        public int currentSeason;
        public int currentYear;
    }
}
