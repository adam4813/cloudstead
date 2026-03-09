using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private SlotUI[] slotUIs;
    private bool isOpen;
    private InputAction _airshipOpenInventory;

    private void Start()
    {
        InitializeSlots();
        Close();
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // Subscribe to Airship map's OpenInventory so it works while piloting
        var playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            _airshipOpenInventory = playerInput.actions.FindActionMap("Airship")?.FindAction("OpenInventory");
            if (_airshipOpenInventory != null)
                _airshipOpenInventory.performed += OnOpenInventory;
        }
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        if (_airshipOpenInventory != null)
            _airshipOpenInventory.performed -= OnOpenInventory;
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

    /// <summary>
    /// Called by PlayerInput UnityEvent. Only toggles on the performed phase
    /// to prevent multiple firings per key press.
    /// </summary>
    public void OnOpenInventory(InputAction.CallbackContext context)
    {
        if (context.performed) Toggle();
    }

    public void Open()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentState != GameState.Playing && gm.CurrentState != GameState.Airship) return;

        isOpen = true;
        if (panel != null) panel.SetActive(true);
        RefreshSlots();
        if (openSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(openSound, Camera.main.transform.position);
        GameManager.Instance?.SetState(GameState.Menu);
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
        if (closeSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(closeSound, Camera.main.transform.position);
        GameManager.Instance?.SetState(GameState.Playing);
        //GameManager.Instance?.RestorePreviousState();
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
