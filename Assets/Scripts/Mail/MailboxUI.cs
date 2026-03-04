using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Letter list + detail reader panel for the mailbox.
/// Left panel: scrollable letter list. Right panel: selected letter body.
/// </summary>
public class MailboxUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject mailPanel;

    [Header("Letter List (left)")]
    [SerializeField] private Transform letterListContent;
    [SerializeField] private GameObject letterEntryPrefab;

    [Header("Detail (right)")]
    [SerializeField] private TextMeshProUGUI senderText;
    [SerializeField] private TextMeshProUGUI subjectText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;

    private bool _isOpen;
    private string _selectedLetterId;
    private readonly List<GameObject> _entryInstances = new();

    private void Start()
    {
        if (mailPanel != null)
            mailPanel.SetActive(false);
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (mailPanel != null)
            mailPanel.SetActive(true);

        GameManager.Instance?.SetState(GameState.Menu);
        RefreshList();
        ClearDetail();
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (mailPanel != null)
            mailPanel.SetActive(false);

        GameManager.Instance?.SetState(GameState.Playing);
    }

    private void RefreshList()
    {
        // Clear old entries
        foreach (var go in _entryInstances)
            Destroy(go);
        _entryInstances.Clear();

        if (MailboxManager.Instance == null || letterListContent == null || letterEntryPrefab == null)
            return;

        var letters = MailboxManager.Instance.Letters;
        // Show newest first
        for (int i = letters.Count - 1; i >= 0; i--)
        {
            var letter = letters[i];
            var entry = Instantiate(letterEntryPrefab, letterListContent);
            _entryInstances.Add(entry);

            // Find text components in the prefab
            var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = letter.sender;
                texts[1].text = letter.subject;
            }
            else if (texts.Length == 1)
            {
                texts[0].text = $"{letter.sender}: {letter.subject}";
            }

            // Bold/highlight unread
            if (!letter.isRead)
            {
                foreach (var t in texts)
                    t.fontStyle = FontStyles.Bold;
            }

            // Click to select
            string letterId = letter.id;
            var btn = entry.GetComponent<Button>();
            if (btn == null)
                btn = entry.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectLetter(letterId));
        }
    }

    private void SelectLetter(string letterId)
    {
        _selectedLetterId = letterId;
        MailboxManager.Instance?.MarkAsRead(letterId);

        var letters = MailboxManager.Instance?.Letters;
        if (letters == null) return;

        LetterData selected = null;
        foreach (var l in letters)
        {
            if (l.id == letterId) { selected = l; break; }
        }

        if (selected == null) return;
        var letter = selected;

        if (senderText != null) senderText.text = $"From: {letter.sender}";
        if (subjectText != null) subjectText.text = letter.subject;
        if (bodyText != null) bodyText.text = letter.body;

        bool hasAttachment = !string.IsNullOrEmpty(letter.attachedItemId);
        if (claimButton != null) claimButton.gameObject.SetActive(hasAttachment);
        if (claimButtonText != null && hasAttachment)
        {
            var db = GameBootstrapper.Database;
            var item = db?.GetItem(letter.attachedItemId);
            string itemName = item != null ? item.itemName : "item";
            claimButtonText.text = $"Claim {itemName} x{letter.attachedItemCount}";
        }

        // Refresh list to update bold/unread state
        RefreshList();
    }

    private void ClearDetail()
    {
        _selectedLetterId = null;
        if (senderText != null) senderText.text = "";
        if (subjectText != null) subjectText.text = "";
        if (bodyText != null) bodyText.text = "Select a letter to read.";
        if (claimButton != null) claimButton.gameObject.SetActive(false);
    }

    private void OnClaimClicked()
    {
        if (string.IsNullOrEmpty(_selectedLetterId)) return;
        if (MailboxManager.Instance?.ClaimAttachment(_selectedLetterId) == true)
        {
            if (claimButton != null) claimButton.gameObject.SetActive(false);
        }
    }
}
