[System.Serializable]
public class InventorySlot
{
    public ItemDefinition item;
    public int count;

    public bool IsEmpty() => item == null || count <= 0;

    public bool CanStack(ItemDefinition other)
    {
        if (IsEmpty()) return true;
        return item == other && count < item.maxStack;
    }

    public int Add(int amount)
    {
        if (item == null) return amount;
        int space = item.maxStack - count;
        int toAdd = UnityEngine.Mathf.Min(amount, space);
        count += toAdd;
        return amount - toAdd; // remainder
    }

    public int Remove(int amount)
    {
        int toRemove = UnityEngine.Mathf.Min(amount, count);
        count -= toRemove;
        if (count <= 0)
        {
            item = null;
            count = 0;
        }
        return toRemove;
    }

    public void Set(ItemDefinition newItem, int newCount)
    {
        item = newItem;
        count = newCount;
    }

    public void Clear()
    {
        item = null;
        count = 0;
    }
}
