using UnityEngine;
using UnityEngine.InputSystem;

public class ToolController : MonoBehaviour
{
    [SerializeField] private ToolDefinition[] debugTools;  // Hoe, WateringCan, Scythe
    [SerializeField] private CropDefinition[] debugCrops;  // Turnip, Potato

    private PlayerController playerController;
    private int activeSlotIndex;
    private bool isUsingTool;

    private int ToolCount => debugTools != null ? debugTools.Length : 0;

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
        activeSlotIndex = index;
        bool isCrop = index >= ToolCount;
        string itemName = isCrop ? GetActiveCrop()?.cropName : GetActiveTool()?.itemName;
        Debug.Log($"[ToolController] Slot {index + 1}: {itemName ?? "empty"}");
    }

    private void UseActiveItem(Vector3Int targetTile)
    {
        if (activeSlotIndex >= ToolCount)
        {
            // Crop planting mode
            var crop = GetActiveCrop();
            if (crop != null)
                FarmingManager.Instance.PlantSeed(targetTile, crop, 0);
            return;
        }

        var tool = GetActiveTool();
        if (tool == null) return;

        switch (tool.toolType)
        {
            case ToolType.Hoe:
                FarmingManager.Instance.TillSoil(targetTile, 0);
                break;
            case ToolType.WateringCan:
                FarmingManager.Instance.WaterPlot(targetTile, 0);
                break;
            case ToolType.Scythe:
                TryHarvest(targetTile);
                break;
        }
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

    private ToolDefinition GetActiveTool()
    {
        if (debugTools == null || activeSlotIndex < 0 || activeSlotIndex >= debugTools.Length)
            return null;
        return debugTools[activeSlotIndex];
    }

    private CropDefinition GetActiveCrop()
    {
        int cropIndex = activeSlotIndex - ToolCount;
        if (debugCrops == null || cropIndex < 0 || cropIndex >= debugCrops.Length)
            return null;
        return debugCrops[cropIndex];
    }
}
