using UnityEngine;
using UnityEngine.InputSystem;

public class ToolController : MonoBehaviour
{
    [SerializeField] private QuickbarUI quickbarUI;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private CropDefinition[] cropRegistry;
    [SerializeField] private AudioClip tillSound;
    [SerializeField] private AudioClip waterSound;
    [SerializeField] private AudioClip harvestSound;
    [SerializeField] private AudioClip plantSound;

    private PlayerController playerController;
    private TileCursor tileCursor;
    private AudioSource audioSource;
    private bool isUsingTool;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        tileCursor = FindFirstObjectByType<TileCursor>();
    }

    // Called by E key (InputAction) — always uses the highlighted tile
    public void OnUseTool(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (isUsingTool) return;

        Vector3Int targetTile = tileCursor != null
            ? tileCursor.HighlightedTile
            : playerController.GetTargetTile();
        UseActiveItem(targetTile);
    }

    // Called by left mouse click (InputAction) — interacts if possible, else uses tool
    public void OnMouseUseTool(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (isUsingTool) return;
        if (tileCursor == null || !tileCursor.IsMouseTargeting) return;

        Vector3Int targetTile = tileCursor.HighlightedTile;

        // Check for a non-PlacedItem interactable at the clicked tile first
        if (TryMouseInteract(targetTile)) return;

        playerController.FaceToward(targetTile);
        UseActiveItem(targetTile);
    }

    private LayerMask EffectiveInteractableMask =>
        InteriorManager.Instance != null
            ? InteriorManager.Instance.GetInteractableMask(interactableMask)
            : interactableMask;

    private bool TryMouseInteract(Vector3Int tile)
    {
        Vector2 center = new Vector2(tile.x + 0.5f, tile.y + 0.5f);
        var hits = Physics2D.OverlapCircleAll(center, 0.4f, EffectiveInteractableMask);
        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable == null || interactable is PlacedItem) continue;
            if (!interactable.CanInteract(0)) continue;

            playerController.FaceToward(tile);
            interactable.Interact(0);
            EventBus.Publish(new InteractionEvent { Target = hit.gameObject });
            return true;
        }
        return false;
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
            if (HasInteractableAtTile(targetTile)) return;
            UseTool(tool, targetTile);
            return;
        }

        if (item.category == ItemCategory.Seed)
        {
            TryPlant(item, targetTile);
            return;
        }
    }

    private bool HasInteractableAtTile(Vector3Int tile)
    {
        Vector2 center = new Vector2(tile.x + 0.5f, tile.y + 0.5f);
        var hits = Physics2D.OverlapCircleAll(center, 0.4f, EffectiveInteractableMask);
        foreach (var hit in hits)
            if (hit.GetComponent<IInteractable>() != null) return true;
        return false;
    }

    private void UseTool(ToolDefinition tool, Vector3Int targetTile)
    {
        bool success = false;
        switch (tool.toolType)
        {
            case ToolType.Hoe:
                success = FarmingManager.Instance.TillSoil(targetTile, 0);
                if (success && tillSound != null) audioSource.PlayOneShot(tillSound);
                break;
            case ToolType.WateringCan:
                success = FarmingManager.Instance.WaterPlot(targetTile, 0);
                if (success && waterSound != null) audioSource.PlayOneShot(waterSound);
                break;
            case ToolType.Scythe:
                TryHarvest(targetTile);
                success = true;
                if (harvestSound != null) audioSource.PlayOneShot(harvestSound);
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
        {
            InventoryManager.Instance.RemoveItem(seedItem, 1);
            if (plantSound != null) audioSource.PlayOneShot(plantSound);
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
