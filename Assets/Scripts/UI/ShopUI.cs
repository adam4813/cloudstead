using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform shopItemsContainer;
    [SerializeField] private Transform playerItemsContainer;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private GameObject playerItemPrefab;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button closeButton;

    private bool isOpen;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
        Hide();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
    }

    public void Open(NPCDefinition merchant)
    {
        if (merchant == null) return;

        isOpen = true;
        if (shopPanel != null) shopPanel.SetActive(true);

        PopulateShopItems(merchant);
        PopulatePlayerItems();
        UpdateGoldDisplay();
    }

    public void Close()
    {
        isOpen = false;
        if (shopPanel != null) shopPanel.SetActive(false);
        ShopManager.Instance?.CloseShop();
    }

    private void PopulateShopItems(NPCDefinition merchant)
    {
        ClearContainer(shopItemsContainer);
        if (merchant.shopInventory == null) return;

        for (int i = 0; i < merchant.shopInventory.Length; i++)
        {
            var item = merchant.shopInventory[i];
            int price = i < merchant.shopPrices.Length ? merchant.shopPrices[i] : 0;

            if (shopItemPrefab != null && shopItemsContainer != null)
            {
                var go = Instantiate(shopItemPrefab, shopItemsContainer);
                SetupShopRow(go, item, price, i);
            }
        }
    }

    private void PopulatePlayerItems()
    {
        ClearContainer(playerItemsContainer);
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < InventoryManager.Instance.SlotCount; i++)
        {
            var slot = InventoryManager.Instance.GetSlot(i);
            if (slot.IsEmpty() || slot.item.sellPrice <= 0) continue;

            if (playerItemPrefab != null && playerItemsContainer != null)
            {
                var go = Instantiate(playerItemPrefab, playerItemsContainer);
                SetupSellRow(go, slot, i);
            }
        }
    }

    private void SetupShopRow(GameObject row, ItemDefinition item, int price, int shopIndex)
    {
        var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 2)
        {
            texts[0].text = item.itemName;
            texts[1].text = $"{price}g";
        }

        var btn = row.GetComponentInChildren<Button>();
        if (btn != null)
        {
            int idx = shopIndex;
            btn.onClick.AddListener(() =>
            {
                ShopManager.Instance?.BuyItem(idx);
                UpdateGoldDisplay();
                PopulatePlayerItems(); // Refresh sell side
            });
        }
    }

    private void SetupSellRow(GameObject row, InventorySlot slot, int inventoryIndex)
    {
        var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 2)
        {
            texts[0].text = $"{slot.item.itemName} x{slot.count}";
            texts[1].text = $"{slot.item.sellPrice}g";
        }

        var btn = row.GetComponentInChildren<Button>();
        if (btn != null)
        {
            int idx = inventoryIndex;
            btn.onClick.AddListener(() =>
            {
                ShopManager.Instance?.SellItem(idx);
                UpdateGoldDisplay();
                PopulatePlayerItems(); // Refresh
            });
        }
    }

    private void UpdateGoldDisplay()
    {
        if (goldText != null && EconomyManager.Instance != null)
            goldText.text = $"Gold: {EconomyManager.Instance.PlayerGold}";
    }

    private void OnGoldChanged(GoldChangedEvent evt)
    {
        if (isOpen)
            UpdateGoldDisplay();
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    private void Hide()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
    }
}
