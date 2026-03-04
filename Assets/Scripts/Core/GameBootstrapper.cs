using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private GameDatabase gameDatabase;

    public static GameDatabase Database { get; private set; }

    private void Start()
    {
        Database = gameDatabase;
        if (Database == null)
            Debug.LogError("[GameBootstrapper] GameDatabase not assigned!");

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
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.Initialize();
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
        if (ResourceNodeManager.Instance != null)
            ResourceNodeManager.Instance.Initialize();
        if (PlacementManager.Instance != null)
            PlacementManager.Instance.Initialize();
        if (InteriorManager.Instance != null)
            InteriorManager.Instance.Initialize();
        if (TileConditionRegistry.Instance != null)
            TileConditionRegistry.Instance.Initialize();
        if (IslandRegistry.Instance != null)
            IslandRegistry.Instance.Initialize();
        if (MailboxManager.Instance != null)
            MailboxManager.Instance.Initialize();
    }
}
