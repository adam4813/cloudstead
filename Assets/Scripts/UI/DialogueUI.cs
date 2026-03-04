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
    private NPCDefinition pendingShopMerchant;

    private void Start()
    {
        EventBus.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
        EventBus.Subscribe<DialogueEndedEvent>(OnDialogueEnded);
        EventBus.Subscribe<ShopGreetingEvent>(OnShopGreeting);
        Hide();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
        EventBus.Unsubscribe<DialogueEndedEvent>(OnDialogueEnded);
        EventBus.Unsubscribe<ShopGreetingEvent>(OnShopGreeting);
    }

    private void Update()
    {
        bool inDialogue = DialogueManager.Instance?.IsActive ?? false;
        bool inShopGreeting = pendingShopMerchant != null;

        if (!inDialogue && !inShopGreeting) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (inShopGreeting)
            {
                if (isTyping)
                {
                    // Skip typewriter
                    isTyping = false;
                    dialogueText.text = fullText;
                    ShowContinuePrompt();
                }
                else
                {
                    // Greeting finished, open shop
                    var merchant = pendingShopMerchant;
                    pendingShopMerchant = null;
                    Hide();
                    ShopManager.Instance?.OpenShop(merchant);
                }
                return;
            }
            if (inDialogue)
                OnAdvance();
        }

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

    private void OnShopGreeting(ShopGreetingEvent evt)
    {
        pendingShopMerchant = evt.Merchant;
        Show();
        ClearChoices();

        if (nameText != null)
            nameText.text = evt.Merchant.npcName;

        if (portraitImage != null && evt.Merchant.portrait != null)
        {
            portraitImage.sprite = evt.Merchant.portrait;
            portraitImage.gameObject.SetActive(true);
        }

        // Start typewriter for the greeting
        fullText = evt.Greeting;
        displayedCharCount = 0;
        isTyping = true;
        typeTimer = 0f;
        if (dialogueText != null) dialogueText.text = "";
        if (continuePrompt != null)
        {
            continuePrompt.text = "Press E to shop";
            continuePrompt.gameObject.SetActive(false);
        }
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
            for (int i = 0; i < node.choiceTexts.Length; i++)
                CreateChoiceButton(node.choiceTexts[i], i);
        }
    }

    private void ShowContinuePrompt()
    {
        if (continuePrompt == null) return;

        if (pendingShopMerchant != null)
        {
            continuePrompt.text = "Press E to shop";
            continuePrompt.gameObject.SetActive(true);
            return;
        }

        var node = DialogueManager.Instance?.CurrentNode;
        bool hasChoices = node?.choiceTexts != null && node.choiceTexts.Length > 0;

        continuePrompt.text = "Press E to continue";
        continuePrompt.gameObject.SetActive(!hasChoices);
    }

    private void ClearChoices()
    {
        if (choiceContainer == null) return;
        for (int i = choiceContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(choiceContainer.GetChild(i).gameObject);
    }

    private void CreateChoiceButton(string text, int index)
    {
        if (choiceButtonPrefab == null || choiceContainer == null) return;

        var btnGO = Instantiate(choiceButtonPrefab, choiceContainer);
        var tmpText = btnGO.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null) tmpText.text = text;

        var btn = btnGO.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSelectChoice(index));
        }
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
