using UnityEngine;

public class ShopManager : Singleton<ShopManager>
{
    [SerializeField] private ShopUI shopUI;

    private NPCDefinition currentMerchant;

    public NPCDefinition CurrentMerchant => currentMerchant;

    public void OpenShop(NPCDefinition merchant)
    {
        if (merchant == null || !merchant.isMerchant) return;

        currentMerchant = merchant;
        GameManager.Instance?.SetState(GameState.Menu);
        if (shopUI != null) shopUI.Open(merchant);
    }

    public bool BuyItem(int shopIndex)
    {
        if (currentMerchant == null) return false;
        if (shopIndex < 0 || shopIndex >= currentMerchant.shopInventory.Length) return false;

        var item = currentMerchant.shopInventory[shopIndex];
        int price = currentMerchant.shopPrices[shopIndex];

        if (!EconomyManager.Instance.SpendGold(price)) return false;
        if (!InventoryManager.Instance.AddItem(item))
        {
            // Refund if inventory full
            EconomyManager.Instance.AddGold(price);
            return false;
        }

        EventBus.Publish(new ItemBoughtEvent { Item = item, Price = price });
        Debug.Log($"[ShopManager] Bought {item.itemName} for {price}g");
        return true;
    }

    public bool SellItem(int inventoryIndex)
    {
        var slot = InventoryManager.Instance?.GetSlot(inventoryIndex);
        if (slot == null || slot.IsEmpty()) return false;

        var item = slot.item;
        int sellPrice = item.sellPrice;
        if (sellPrice <= 0) return false;

        InventoryManager.Instance.RemoveItem(item, 1);
        EconomyManager.Instance.AddGold(sellPrice);

        EventBus.Publish(new ItemSoldEvent { Item = item, Price = sellPrice });
        Debug.Log($"[ShopManager] Sold {item.itemName} for {sellPrice}g");
        return true;
    }

    public void CloseShop()
    {
        currentMerchant = null;
        GameManager.Instance?.RestorePreviousState();
    }
}
