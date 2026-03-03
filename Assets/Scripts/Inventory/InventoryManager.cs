using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : Singleton<InventoryManager>, ISaveable
{
    [SerializeField] private int slotCount = 24;
    [SerializeField] private AudioClip itemPickupSound;

    private InventorySlot[] slots;

    public int SlotCount => slotCount;

    private Dictionary<string, ItemDefinition> itemLookup;

    public override void Initialize()
    {
        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
            slots[i] = new InventorySlot();

        if (SaveManager.Instance != null)
            SaveManager.Instance.Register(this);
    }

    public bool AddItem(ItemDefinition item, int count = 1)
    {
        if (item == null || count <= 0) return false;

        int remaining = count;

        // First try to stack into existing slots
        for (int i = 0; i < slotCount && remaining > 0; i++)
        {
            if (slots[i].item == item && slots[i].CanStack(item))
                remaining = slots[i].Add(remaining);
        }

        // Then fill empty slots
        for (int i = 0; i < slotCount && remaining > 0; i++)
        {
            if (slots[i].IsEmpty())
            {
                slots[i].Set(item, 0);
                remaining = slots[i].Add(remaining);
            }
        }

        if (remaining < count)
        {
            EventBus.Publish(new InventoryChangedEvent());
            EventBus.Publish(new ItemPickedUpEvent { Item = item, Count = count - remaining });
            if (itemPickupSound != null && Camera.main != null)
                AudioSource.PlayClipAtPoint(itemPickupSound, Camera.main.transform.position);
        }

        return remaining == 0;
    }

    public bool RemoveItem(ItemDefinition item, int count = 1)
    {
        if (!HasItem(item, count)) return false;

        int remaining = count;
        for (int i = slotCount - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i].item == item)
                remaining -= slots[i].Remove(remaining);
        }

        EventBus.Publish(new InventoryChangedEvent());
        return true;
    }

    public bool HasItem(ItemDefinition item, int count = 1)
    {
        int total = 0;
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i].item == item)
                total += slots[i].count;
        }
        return total >= count;
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slotCount) return null;
        return slots[index];
    }

    public void SwapSlots(int a, int b)
    {
        if (a < 0 || a >= slotCount || b < 0 || b >= slotCount) return;

        var temp = new InventorySlot();
        temp.Set(slots[a].item, slots[a].count);

        slots[a].Set(slots[b].item, slots[b].count);
        slots[b].Set(temp.item, temp.count);

        EventBus.Publish(new InventoryChangedEvent());
    }

    public int GetItemCount(ItemDefinition item)
    {
        int total = 0;
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i].item == item)
                total += slots[i].count;
        }
        return total;
    }

    public string SaveState()
    {
        var data = new InventorySaveData
        {
            slots = new SlotSaveData[slotCount]
        };
        for (int i = 0; i < slotCount; i++)
        {
            data.slots[i] = new SlotSaveData
            {
                itemId = slots[i].item != null ? slots[i].item.ItemId : "",
                count = slots[i].count
            };
        }
        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<InventorySaveData>(json);
        if (data?.slots == null) return;

        BuildItemLookup();

        for (int i = 0; i < slotCount && i < data.slots.Length; i++)
        {
            var slotData = data.slots[i];
            if (string.IsNullOrEmpty(slotData.itemId) || slotData.count <= 0)
            {
                slots[i].Clear();
            }
            else if (itemLookup.TryGetValue(slotData.itemId, out var item))
            {
                slots[i].Set(item, slotData.count);
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] Item '{slotData.itemId}' not found during load");
                slots[i].Clear();
            }
        }

        EventBus.Publish(new InventoryChangedEvent());
    }

    private void BuildItemLookup()
    {
        if (itemLookup != null) return;
        itemLookup = new Dictionary<string, ItemDefinition>();
        var allItems = Resources.LoadAll<ItemDefinition>("");
        foreach (var item in allItems)
            itemLookup[item.ItemId] = item;
    }

    [System.Serializable]
    private class InventorySaveData
    {
        public SlotSaveData[] slots;
    }

    [System.Serializable]
    private class SlotSaveData
    {
        public string itemId;
        public int count;
    }
}
