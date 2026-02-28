using UnityEngine;
using Sirenix.OdinInspector;
using System;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Cloudstead/NPC/Dialogue Tree")]
public class DialogueTree : ScriptableObject
{
    [ListDrawerSettings(ShowIndexLabels = true)]
    public DialogueNode[] nodes;

    public DialogueNode GetNode(int index)
    {
        if (nodes == null || index < 0 || index >= nodes.Length) return null;
        return nodes[index];
    }

    public bool IsValid => nodes != null && nodes.Length > 0;

    [Serializable]
    public class DialogueNode
    {
        public string speakerName;

        [TextArea(2, 5)]
        public string text;

        [Tooltip("If empty, this is a linear node (uses nextIndex). If populated, player has choices.")]
        public string[] choiceTexts;

        [Tooltip("Node index each choice leads to. Must match choiceTexts length.")]
        public int[] choiceNextIndices;

        [Tooltip("Next node index for linear progression. -1 = end dialogue.")]
        public int nextIndex = -1;

        public bool HasChoices => choiceTexts != null && choiceTexts.Length > 0;
        public bool IsEnd => !HasChoices && nextIndex < 0;
    }
}
