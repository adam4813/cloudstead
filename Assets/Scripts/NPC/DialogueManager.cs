using UnityEngine;

public class DialogueManager : Singleton<DialogueManager>
{
    private DialogueTree currentTree;
    private NPCDefinition currentSpeaker;
    private int currentNodeIndex;
    private bool isActive;

    public bool IsActive => isActive;

    /// <summary>Optional one-shot callback invoked when a dialogue choice is selected. Cleared after invocation.</summary>
    public System.Action<int> PendingChoiceCallback { get; set; }
    public DialogueTree.DialogueNode CurrentNode =>
        currentTree != null && currentNodeIndex >= 0 && currentNodeIndex < currentTree.nodes.Length
            ? currentTree.nodes[currentNodeIndex]
            : null;

    public void StartDialogue(DialogueTree tree, NPCDefinition speaker)
    {
        if (tree == null || tree.nodes == null || tree.nodes.Length == 0) return;

        currentTree = tree;
        currentSpeaker = speaker;
        currentNodeIndex = 0;
        isActive = true;

        GameManager.Instance?.SetState(GameState.Dialogue);
        EventBus.Publish(new DialogueStartedEvent { SpeakerName = speaker?.npcName ?? "" });
    }

    public void AdvanceDialogue()
    {
        if (!isActive) return;

        var node = CurrentNode;
        if (node == null) return;

        // Choices take priority — never auto-advance past a choice node
        if (node.choiceTexts != null && node.choiceTexts.Length > 0)
            return;

        if (node.nextIndex == -1)
        {
            EndDialogue();
            return;
        }

        currentNodeIndex = node.nextIndex;
        if (currentNodeIndex < 0 || currentNodeIndex >= currentTree.nodes.Length)
            EndDialogue();
    }

    public void SelectChoice(int choiceIndex)
    {
        if (!isActive) return;

        // Fire one-shot callback if set
        var callback = PendingChoiceCallback;
        PendingChoiceCallback = null;
        callback?.Invoke(choiceIndex);

        var node = CurrentNode;
        if (node == null) return;
        if (node.choiceNextIndices == null || choiceIndex >= node.choiceNextIndices.Length)
        {
            // No valid next index for this choice — end dialogue
            EndDialogue();
            return;
        }

        int nextIndex = node.choiceNextIndices[choiceIndex];
        if (nextIndex < 0 || nextIndex >= currentTree.nodes.Length || nextIndex == currentNodeIndex)
        {
            // Invalid or self-loop — end dialogue
            EndDialogue();
            return;
        }

        currentNodeIndex = nextIndex;
    }

    public void EndDialogue()
    {
        if (!isActive) return;

        isActive = false;
        currentTree = null;
        currentSpeaker = null;
        currentNodeIndex = -1;

        EventBus.Publish(new DialogueEndedEvent());
        GameManager.Instance?.SetState(GameState.Playing);
    }
}
