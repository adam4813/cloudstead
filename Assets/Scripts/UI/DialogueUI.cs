using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private TextMeshProUGUI continuePrompt;
    [SerializeField] private float typewriterSpeed = 30f;

    private string fullText;
    private int displayedCharCount;
    private bool isTyping;
    private float typeTimer;

    private void Start()
    {
        EventBus.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
        EventBus.Subscribe<DialogueEndedEvent>(OnDialogueEnded);
        Hide();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
        EventBus.Unsubscribe<DialogueEndedEvent>(OnDialogueEnded);
    }

    private void Update()
    {
        if (!DialogueManager.Instance?.IsActive ?? true) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            OnAdvance();

        if (isTyping)
        {
            typeTimer += Time.deltaTime * typewriterSpeed;
            if (typeTimer >= 1f)
            {
                typeTimer -= 1f;
                displayedCharCount++;
                if (displayedCharCount >= fullText.Length)
                {
                    isTyping = false;
                    dialogueText.text = fullText;
                    ShowContinuePrompt();
                }
                else
                {
                    dialogueText.text = fullText.Substring(0, displayedCharCount);
                }
            }
        }
    }

    private void OnDialogueStarted(DialogueStartedEvent evt)
    {
        Show();
        DisplayCurrentNode();
    }

    private void OnDialogueEnded(DialogueEndedEvent evt)
    {
        Hide();
    }

    public void OnAdvance()
    {
        if (isTyping)
        {
            // Skip typewriter — show full text
            isTyping = false;
            dialogueText.text = fullText;
            ShowContinuePrompt();
        }
        else
        {
            DialogueManager.Instance?.AdvanceDialogue();
            if (DialogueManager.Instance?.IsActive ?? false)
                DisplayCurrentNode();
        }
    }

    public void OnSelectChoice(int index)
    {
        DialogueManager.Instance?.SelectChoice(index);
        if (DialogueManager.Instance?.IsActive ?? false)
            DisplayCurrentNode();
    }

    private void DisplayCurrentNode()
    {
        var node = DialogueManager.Instance?.CurrentNode;
        if (node == null) return;

        if (nameText != null)
            nameText.text = node.speakerName;

        // Start typewriter effect
        fullText = node.text;
        displayedCharCount = 0;
        isTyping = true;
        typeTimer = 0f;
        dialogueText.text = "";

        if (continuePrompt != null)
            continuePrompt.gameObject.SetActive(false);

        // Set up choices
        ClearChoices();
        if (node.choiceTexts != null && node.choiceTexts.Length > 0)
        {
            foreach (var choice in node.choiceTexts)
                CreateChoiceButton(choice);
        }
    }

    private void ShowContinuePrompt()
    {
        var node = DialogueManager.Instance?.CurrentNode;
        bool hasChoices = node?.choiceTexts != null && node.choiceTexts.Length > 0;

        if (continuePrompt != null)
            continuePrompt.gameObject.SetActive(!hasChoices);
    }

    private void ClearChoices()
    {
        if (choiceContainer == null) return;
        for (int i = choiceContainer.childCount - 1; i >= 0; i--)
            Destroy(choiceContainer.GetChild(i).gameObject);
    }

    private void CreateChoiceButton(string text)
    {
        if (choiceButtonPrefab == null || choiceContainer == null) return;

        var btnGO = Instantiate(choiceButtonPrefab, choiceContainer);
        var tmpText = btnGO.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null) tmpText.text = text;

        int index = choiceContainer.childCount - 1;
        var btn = btnGO.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => OnSelectChoice(index));
    }

    private void Show()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
    }

    private void Hide()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }
}
