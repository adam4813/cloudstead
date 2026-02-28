using UnityEngine;

public class InventoryManager : Singleton<InventoryManager>
{
    [SerializeField] private int slotCount = 24;

    private InventorySlot[] slots;

    public int SlotCount => slotCount;

    public override void Initialize()
    {
        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
            slots[i] = new InventorySlot();
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
}
