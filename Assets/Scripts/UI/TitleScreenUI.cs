using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleScreenUI : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Save Slots")]
    [SerializeField] private SaveSlotUI[] saveSlots = new SaveSlotUI[3];

    [Header("Confirm Delete Panel")]
    [SerializeField] private GameObject confirmDeletePanel;
    [SerializeField] private TextMeshProUGUI confirmDeleteText;
    [SerializeField] private Button confirmDeleteYesButton;
    [SerializeField] private Button confirmDeleteNoButton;

    private string pendingDeleteSlot;

    [System.Serializable]
    public class SaveSlotUI
    {
        public string slotName;
        public TextMeshProUGUI slotLabel;
        public Button playButton;
        public Button deleteButton;
    }

    private void Start()
    {
        if (titleText != null)
            titleText.text = "Cloudstead";

        if (confirmDeletePanel != null)
            confirmDeletePanel.SetActive(false);

        for (int i = 0; i < saveSlots.Length; i++)
        {
            var slot = saveSlots[i];
            if (string.IsNullOrEmpty(slot.slotName))
                slot.slotName = $"slot{i + 1}";

            string captured = slot.slotName;
            slot.playButton?.onClick.AddListener(() => OnPlay(captured));
            slot.deleteButton?.onClick.AddListener(() => OnDeleteRequest(captured));
        }

        confirmDeleteYesButton?.onClick.AddListener(OnConfirmDelete);
        confirmDeleteNoButton?.onClick.AddListener(OnCancelDelete);

        RefreshSlots();
    }

    private void RefreshSlots()
    {
        if (SaveManager.Instance == null) return;

        foreach (var slot in saveSlots)
        {
            var meta = SaveManager.Instance.GetSlotMetadata(slot.slotName);

            if (meta.exists)
            {
                slot.slotLabel.text = $"Continue - Day {meta.day}, {meta.season}";
                if (slot.deleteButton != null)
                    slot.deleteButton.gameObject.SetActive(true);
            }
            else
            {
                slot.slotLabel.text = "New Game";
                if (slot.deleteButton != null)
                    slot.deleteButton.gameObject.SetActive(false);
            }
        }
    }

    private void OnPlay(string slotName)
    {
        if (SaveManager.Instance == null || SceneTransitionManager.Instance == null) return;

        SaveManager.Instance.SetActiveSlot(slotName);

        if (SaveManager.Instance.SlotExists(slotName))
            SceneTransitionManager.Instance.LoadSceneFromSave(slotName, "Farm");
        else
            SceneTransitionManager.Instance.StartNewGame("Farm");
    }

    private void OnDeleteRequest(string slotName)
    {
        pendingDeleteSlot = slotName;

        if (confirmDeletePanel != null)
        {
            confirmDeletePanel.SetActive(true);
            if (confirmDeleteText != null)
                confirmDeleteText.text = $"Delete save \"{slotName}\"?\nAre you sure?";
        }
    }

    private void OnConfirmDelete()
    {
        if (!string.IsNullOrEmpty(pendingDeleteSlot) && SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSlot(pendingDeleteSlot);
            Debug.Log($"[TitleScreenUI] Deleted save slot: {pendingDeleteSlot}");
        }

        pendingDeleteSlot = null;
        if (confirmDeletePanel != null)
            confirmDeletePanel.SetActive(false);

        RefreshSlots();
    }

    private void OnCancelDelete()
    {
        pendingDeleteSlot = null;
        if (confirmDeletePanel != null)
            confirmDeletePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (var slot in saveSlots)
        {
            slot.playButton?.onClick.RemoveAllListeners();
            slot.deleteButton?.onClick.RemoveAllListeners();
        }

        confirmDeleteYesButton?.onClick.RemoveAllListeners();
        confirmDeleteNoButton?.onClick.RemoveAllListeners();
    }
}
