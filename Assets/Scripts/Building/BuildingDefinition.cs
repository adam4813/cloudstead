using UnityEngine;
using Sirenix.OdinInspector;

public enum BuildingType { Shop, Home, Service, Public, Decorative }

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Cloudstead/Building/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    [FoldoutGroup("Identity")]
    [Required]
    public string buildingName;

    [FoldoutGroup("Identity")]
    public BuildingType buildingType;

    [FoldoutGroup("Identity")]
    [TextArea(1, 3)]
    public string description;

    [FoldoutGroup("Identity")]
    [Tooltip("NPC who owns/runs this building. Null for public structures.")]
    public NPCDefinition ownerNPC;

    [FoldoutGroup("Identity")]
    [PreviewField(64)]
    public Sprite icon;

    [FoldoutGroup("Structure")]
    [Tooltip("Prefab containing exterior tilemaps, colliders, and DoorTrigger. Must have a BuildingStructure component on the root.")]
    [AssetsOnly]
    public GameObject exteriorPrefab;

    [FoldoutGroup("Structure")]
    [Tooltip("If true, the exterior prefab includes a BuildingInterior and door. If false, the building is exterior-only (e.g. well, shrine).")]
    public bool hasInterior = true;

    [FoldoutGroup("Schedule")]
    [Tooltip("If true, building is always accessible regardless of hour/day.")]
    public bool alwaysOpen = true;

    [FoldoutGroup("Schedule")]
    [HideIf("alwaysOpen")]
    [Range(0f, 24f)]
    [Tooltip("Hour the building opens (24h format, e.g. 9 = 9 AM).")]
    public float openHour = 9f;

    [FoldoutGroup("Schedule")]
    [HideIf("alwaysOpen")]
    [Range(0f, 24f)]
    [Tooltip("Hour the building closes (24h format, e.g. 17 = 5 PM).")]
    public float closeHour = 17f;

    [FoldoutGroup("Schedule")]
    [HideIf("alwaysOpen")]
    [Tooltip("Which days the building is open (index 0 = day 1 of the season). All true by default.")]
    public bool[] daysOpen = { true, true, true, true, true, true, true };

    /// <summary>
    /// Checks whether this building is currently open.
    /// </summary>
    /// <param name="currentHour">Current hour in 24h format (0-24).</param>
    /// <param name="dayOfSeason">Current day within the season (1-based).</param>
    public bool IsOpen(float currentHour, int dayOfSeason)
    {
        if (alwaysOpen) return true;

        // Check day-of-season against the daysOpen pattern (wraps cyclically)
        if (daysOpen != null && daysOpen.Length > 0)
        {
            int index = (dayOfSeason - 1) % daysOpen.Length;
            if (!daysOpen[index]) return false;
        }

        // Handle overnight hours (e.g. open 20, close 4)
        if (openHour <= closeHour)
            return currentHour >= openHour && currentHour < closeHour;
        else
            return currentHour >= openHour || currentHour < closeHour;
    }

    /// <summary>
    /// Convenience: checks IsOpen against the current TimeManager state.
    /// </summary>
    public bool IsOpenNow()
    {
        if (alwaysOpen) return true;
        if (TimeManager.Instance == null) return true;
        return IsOpen(TimeManager.Instance.GetCurrentHour(), TimeManager.Instance.CurrentDay);
    }
}
