using UnityEngine;
using UnityEngine.InputSystem;

public class ToolController : MonoBehaviour
{
    [SerializeField] private QuickbarUI quickbarUI;
    [SerializeField] private CropDefinition[] cropRegistry;

    private PlayerController playerController;
    private bool isUsingTool;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void OnUseTool(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (isUsingTool) return;

        Vector3Int targetTile = playerController.GetTargetTile();
        UseActiveItem(targetTile);
    }

    public void OnQuickbarSlot1(InputAction.CallbackContext context)
    {
        if (context.performed) SelectSlot(0);
    }

    public void OnQuickbarSlot2(InputAction.CallbackContext context)
    {
        if (context.performed) SelectSlot(1);
    }

    public void OnQuickbarSlot3(InputAction.CallbackContext context)
    {
        if (context.performed) SelectSlot(2);
    }

    public void OnQuickbarSlot4(InputAction.CallbackContext context)
    {
        if (context.performed) SelectSlot(3);
    }

    public void OnQuickbarSlot5(InputAction.CallbackContext context)
    {
        if (context.performed) SelectSlot(4);
    }

    private void SelectSlot(int index)
    {
        if (quickbarUI != null)
            quickbarUI.SelectSlot(index);
    }

    private void UseActiveItem(Vector3Int targetTile)
    {
        if (quickbarUI == null) return;
        var slotData = quickbarUI.GetActiveSlotData();
        if (slotData == null || slotData.IsEmpty()) return;

        var item = slotData.item;

        if (item is ToolDefinition tool)
        {
            UseTool(tool, targetTile);
            return;
        }

        if (item.category == ItemCategory.Seed)
        {
            TryPlant(item, targetTile);
            return;
        }
    }

    private void UseTool(ToolDefinition tool, Vector3Int targetTile)
    {
        bool success = false;
        switch (tool.toolType)
        {
            case ToolType.Hoe:
                success = FarmingManager.Instance.TillSoil(targetTile, 0);
                break;
            case ToolType.WateringCan:
                success = FarmingManager.Instance.WaterPlot(targetTile, 0);
                break;
            case ToolType.Scythe:
                TryHarvest(targetTile);
                success = true;
                break;
        }

        if (success)
        {
            var stamina = GetComponent<StaminaController>();
            if (stamina != null)
                stamina.UseStamina(tool.staminaCost);
        }
    }

    private void TryPlant(ItemDefinition seedItem, Vector3Int targetTile)
    {
        var crop = FindCropForSeed(seedItem);
        if (crop == null) return;

        if (FarmingManager.Instance.PlantSeed(targetTile, crop, 0))
            InventoryManager.Instance.RemoveItem(seedItem, 1);
    }

    private void TryHarvest(Vector3Int targetTile)
    {
        if (FarmingManager.Instance.HarvestCrop(targetTile, 0, out var outputs))
        {
            foreach (var output in outputs)
            {
                if (output?.item == null) continue;
                int yield = Random.Range(output.minYield, output.maxYield + 1);
                if (yield <= 0) continue;
                if (InventoryManager.Instance != null)
                    InventoryManager.Instance.AddItem(output.item, yield);
                Debug.Log($"[ToolController] Harvested {yield}x {output.item.itemName}");
            }
        }
        else
        {
            Debug.Log("[ToolController] Nothing to harvest here");
        }
    }

    private CropDefinition FindCropForSeed(ItemDefinition seed)
    {
        if (cropRegistry == null) return null;
        foreach (var crop in cropRegistry)
        {
            if (crop != null && crop.seedItem == seed)
                return crop;
        }
        return null;
    }
}
