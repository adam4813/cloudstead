using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameBootstrapper : MonoBehaviour
{
    private void Start()
    {
        InitializeManagers();
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void InitializeManagers()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Initialize();
        if (TimeManager.Instance != null)
            TimeManager.Instance.Initialize();
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.Initialize();
        if (FarmingManager.Instance != null)
            FarmingManager.Instance.Initialize();
        if (TileManager.Instance != null)
            TileManager.Instance.Initialize();
        if (SaveManager.Instance != null)
            SaveManager.Instance.Initialize();
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.Initialize();
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.Initialize();
        if (ShopManager.Instance != null)
            ShopManager.Instance.Initialize();
        if (SkyWorldManager.Instance != null)
            SkyWorldManager.Instance.Initialize();
        if (CraftingManager.Instance != null)
            CraftingManager.Instance.Initialize();
        if (PlacementManager.Instance != null)
            PlacementManager.Instance.Initialize();
    }
}
