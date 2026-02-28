using UnityEngine;

public class DialogueManager : Singleton<DialogueManager>
{
    private DialogueTree currentTree;
    private NPCDefinition currentSpeaker;
    private int currentNodeIndex;
    private bool isActive;

    public bool IsActive => isActive;
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
        if (node == null || node.nextIndex == -1)
        {
            EndDialogue();
            return;
        }

        // If node has choices, don't auto-advance
        if (node.choiceTexts != null && node.choiceTexts.Length > 0)
            return;

        currentNodeIndex = node.nextIndex;
        if (currentNodeIndex < 0 || currentNodeIndex >= currentTree.nodes.Length)
            EndDialogue();
    }

    public void SelectChoice(int choiceIndex)
    {
        if (!isActive) return;

        var node = CurrentNode;
        if (node == null) return;
        if (node.choiceNextIndices == null || choiceIndex >= node.choiceNextIndices.Length) return;

        int nextIndex = node.choiceNextIndices[choiceIndex];
        if (nextIndex < 0 || nextIndex >= currentTree.nodes.Length)
        {
            EndDialogue();

            // If the choice leads to shop opening
            if (currentSpeaker != null && currentSpeaker.isMerchant)
                ShopManager.Instance?.OpenShop(currentSpeaker);
            return;
        }

        currentNodeIndex = nextIndex;
    }

    public void EndDialogue()
    {
        isActive = false;
        currentTree = null;
        currentSpeaker = null;
        currentNodeIndex = -1;

        EventBus.Publish(new DialogueEndedEvent());
        GameManager.Instance?.SetState(GameState.Playing);
    }
}
