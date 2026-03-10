using System.Collections;
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
        // Skip when loading from a save — inventory will be restored by SaveManager
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoadPending) return;

        // Set the player's initial cloud context
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.CurrentCloud == null)
        {
            var homeCloud = FindFirstObjectByType<CloudIsland>();
            if (homeCloud != null) player.CurrentCloud = homeCloud;
        }

        // Only give starting items if inventory is empty (first load / no save)
        if (InventoryManager.Instance != null)
        {
            var firstSlot = InventoryManager.Instance.GetSlot(0);
            if (firstSlot.IsEmpty())
            {
                if (hoe != null) InventoryManager.Instance.AddItem(hoe);
                if (wateringCan != null) InventoryManager.Instance.AddItem(wateringCan);
                if (scythe != null) InventoryManager.Instance.AddItem(scythe);
                if (turnipSeed != null) InventoryManager.Instance.AddItem(turnipSeed, turnipSeedCount);
                if (potatoSeed != null) InventoryManager.Instance.AddItem(potatoSeed, potatoSeedCount);
                if (sunflowerSeed != null) InventoryManager.Instance.AddItem(sunflowerSeed, sunflowerSeedCount);
                Debug.Log("[FarmStartSetup] Starting inventory populated.");
            }
        }

        // Defer new-game event by one frame so all Start() subscriptions are active
        StartCoroutine(PublishNewGameNextFrame());
    }

    private IEnumerator PublishNewGameNextFrame()
    {
        yield return null;

        if (GameManager.Instance != null)
        {
            var settings = GameManager.Instance.Settings.Clone();
            settings.isNewGame = true;
            GameManager.Instance.ApplySettings(settings);
            EventBus.Publish(new NewGameStartedEvent());
        }
    }
}
