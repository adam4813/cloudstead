using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;

    private SlotUI[] slotUIs;
    private bool isOpen;

    private void Start()
    {
        InitializeSlots();
        Close();
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
    }

    private void InitializeSlots()
    {
        if (InventoryManager.Instance == null) return;

        int count = InventoryManager.Instance.SlotCount;
        slotUIs = new SlotUI[count];

        // Clear existing slots if any
        if (slotsContainer != null)        {
            foreach (Transform child in slotsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < count; i++)
        {
            GameObject slotGO;
            if (slotPrefab != null && slotsContainer != null)
            {
                slotGO = Instantiate(slotPrefab, slotsContainer);
            }
            else
            {
                // Use existing children if no prefab
                slotGO = slotsContainer != null && i < slotsContainer.childCount
                    ? slotsContainer.GetChild(i).gameObject
                    : null;
            }

            if (slotGO != null)
            {
                slotUIs[i] = slotGO.GetComponent<SlotUI>();
                if (slotUIs[i] != null)
                    slotUIs[i].Setup(i);
            }
        }
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        isOpen = true;
        if (panel != null) panel.SetActive(true);
        RefreshSlots();
        GameManager.Instance?.SetState(GameState.Menu);
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
        GameManager.Instance?.SetState(GameState.Playing);
    }

    private void RefreshSlots()
    {
        if (slotUIs == null || InventoryManager.Instance == null) return;

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] != null)
                slotUIs[i].SetSlot(InventoryManager.Instance.GetSlot(i));
        }
    }

    private void OnInventoryChanged(InventoryChangedEvent evt)
    {
        if (isOpen)
            RefreshSlots();
    }
}
