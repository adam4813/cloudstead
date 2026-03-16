using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorTrigger : MonoBehaviour
{
    [Tooltip("The building structure this door belongs to. Auto-discovered from parent if null.")]
    [SerializeField] private BuildingStructure structure;
    [SerializeField] private bool isExitDoor;
    [SerializeField] private AudioClip doorSound;

    [Header("Closed Hours")]
    [SerializeField] private string closedMessage = "Closed right now. Come back later!";

    private void Awake()
    {
        if (structure == null)
            structure = GetComponentInParent<BuildingStructure>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (InteriorManager.Instance == null) return;

        if (isExitDoor)
        {
            PlayDoorSound();
            InteriorManager.Instance.ExitInterior();
            return;
        }

        // Check building hours before entry
        var def = structure != null ? structure.Definition : null;
        if (def != null && !def.IsOpenNow())
        {
            ShowClosedMessage(def);
            return;
        }

        var interior = structure != null ? structure.Interior : null;
        if (interior == null) return;

        PlayDoorSound();
        InteriorManager.Instance.EnterInterior(interior);
    }

    private void PlayDoorSound()
    {
        if (doorSound != null)
            AudioSource.PlayClipAtPoint(doorSound, transform.position);
    }

    private void ShowClosedMessage(BuildingDefinition def)
    {
        string msg = closedMessage;

        if (def != null && !def.alwaysOpen)
            msg = $"{def.buildingName} is closed. Opens at {FormatHour(def.openHour)}.";

        // Use dialogue system for the message so player sees it on screen
        if (DialogueManager.Instance != null)
        {
            var tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.nodes = new[]
            {
                new DialogueTree.DialogueNode
                {
                    speakerName = "",
                    text = msg,
                    nextIndex = -1
                }
            };
            DialogueManager.Instance.StartDialogue(tree, null);
        }
        else
        {
            Debug.Log($"[DoorTrigger] {msg}");
        }
    }

    private static string FormatHour(float hour)
    {
        int h = Mathf.FloorToInt(hour);
        int m = Mathf.FloorToInt((hour - h) * 60f);
        string period = h >= 12 ? "PM" : "AM";
        int display = h > 12 ? h - 12 : (h == 0 ? 12 : h);
        return m > 0 ? $"{display}:{m:D2} {period}" : $"{display} {period}";
    }
}
