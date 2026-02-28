using UnityEngine;

public class FarmStartSetup : MonoBehaviour
{
    [SerializeField] private ItemDefinition turnipSeed;
    [SerializeField] private ItemDefinition potatoSeed;
    [SerializeField] private ItemDefinition sunflowerSeed;
    [SerializeField] private ToolDefinition hoe;
    [SerializeField] private ToolDefinition wateringCan;
    [SerializeField] private ToolDefinition scythe;

    [SerializeField] private int turnipSeedCount = 15;
    [SerializeField] private int potatoSeedCount = 10;
    [SerializeField] private int sunflowerSeedCount = 5;

    private void Start()
    {
        // Only give starting items if inventory is empty (first load / no save)
        if (InventoryManager.Instance == null) return;

        var firstSlot = InventoryManager.Instance.GetSlot(0);
        if (!firstSlot.IsEmpty()) return; // Already has items, skip

        if (hoe != null)
            InventoryManager.Instance.AddItem(hoe);
        if (wateringCan != null)
            InventoryManager.Instance.AddItem(wateringCan);
        if (scythe != null)
            InventoryManager.Instance.AddItem(scythe);
        if (turnipSeed != null)
            InventoryManager.Instance.AddItem(turnipSeed, turnipSeedCount);
        if (potatoSeed != null)
            InventoryManager.Instance.AddItem(potatoSeed, potatoSeedCount);
        if (sunflowerSeed != null)
            InventoryManager.Instance.AddItem(sunflowerSeed, sunflowerSeedCount);

        Debug.Log("[FarmStartSetup] Starting inventory populated.");
    }
}
