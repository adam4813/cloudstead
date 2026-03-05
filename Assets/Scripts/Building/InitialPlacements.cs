using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Defines items that should be placed when a cloud or interior is first created.
/// Attach to any CloudIsland or BuildingInterior GameObject. On Start, each
/// entry is placed through the standard PlacementManager flow (SpawnItemAt).
/// Skipped when loading a save — PlacementManager.RestoreState handles that.
/// </summary>
public class InitialPlacements : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        [Required] public ItemDefinition item;
        public Vector3Int gridPosition;
    }

    [SerializeField] private List<Entry> placements = new();

    private void Start()
    {
        // Skip when loading from a save — placed items are restored by PlacementManager
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoadPending) return;
        if (PlacementManager.Instance == null) return;

        foreach (var entry in placements)
        {
            if (entry.item == null) continue;
            PlacementManager.Instance.SpawnItemAt(entry.item, entry.gridPosition);
        }
    }
}
