using UnityEngine;

public class QuickbarUI : MonoBehaviour
{
    [SerializeField] private SlotUI[] quickbarSlots;

    private int activeSlot;

    public int ActiveSlot => activeSlot;

    private void Start()
    {
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        RefreshSlots();
        UpdateHighlight();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= quickbarSlots.Length) return;
        activeSlot = index;
        UpdateHighlight();
        NotifyPlacementManager();
    }

    private void NotifyPlacementManager()
    {
        if (PlacementManager.Instance == null) return;
        var slot = GetActiveSlotData();
        // Tile-based items (airshipTile) are handled by AirshipBuildMode, not PlacementManager
        if (slot != null && !slot.IsEmpty() && slot.item != null
            && slot.item.isPlaceable && slot.item.airshipTile == null)
            PlacementManager.Instance.EnterPlacementMode(slot.item);
        else
            PlacementManager.Instance.ExitPlacementMode();
    }

    public InventorySlot GetActiveSlotData()
    {
        if (InventoryManager.Instance == null) return null;
        return InventoryManager.Instance.GetSlot(activeSlot);
    }

    private void RefreshSlots()
    {
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < quickbarSlots.Length; i++)
        {
            if (quickbarSlots[i] != null)
            {
                quickbarSlots[i].Setup(i);
                quickbarSlots[i].SetSlot(InventoryManager.Instance.GetSlot(i));
            }
        }
    }

    private void UpdateHighlight()
    {
        for (int i = 0; i < quickbarSlots.Length; i++)
        {
            if (quickbarSlots[i] != null)
                quickbarSlots[i].SetHighlight(i == activeSlot);
        }
    }

    private void OnInventoryChanged(InventoryChangedEvent evt)
    {
        RefreshSlots();
        UpdateHighlight();
    }
}
