using UnityEngine;
using Sirenix.OdinInspector;

public class NPCController : MonoBehaviour, IInteractable, ISaveable
{
    [SerializeField] private NPCDefinition definition;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private DialogueTree greetingDialogue;

    [Header("Waypoints (fallback if no schedule)")]
    [SerializeField] private Transform[] scheduleWaypoints;

    [Header("Time-Based Schedule")]
    [Tooltip("If populated, NPC swaps active waypoints based on the current hour. Evaluated top-to-bottom; first matching entry wins.")]
    [SerializeField] private NPCScheduleEntry[] schedule;

    [Header("Friendship")]
    [SerializeField] private int friendshipPoints;
    [Tooltip("Extra dialogue trees unlocked at friendship thresholds (index 0 = threshold 1, etc.)")]
    [SerializeField] private FriendshipDialogue[] friendshipDialogues;

    private SpriteRenderer spriteRenderer;
    private int currentWaypointIndex;
    private Transform[] _activeWaypoints;
    private float _lastScheduleHour = -1f;

    // Multiplayer readiness — will be assigned when networking is added
    [System.NonSerialized] public uint ownerId;

    public NPCDefinition Definition => definition;
    public int FriendshipPoints => friendshipPoints;

    /// <summary>Unique save key per NPC instance, based on definition name.</summary>
    public string SaveKey => $"NPC_{(definition != null ? definition.npcName : name)}";

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _activeWaypoints = scheduleWaypoints;
        SaveManager.Instance?.Register(this);
        EventBus.Subscribe<GiftGivenEvent>(OnGiftGiven);
        EventBus.Subscribe<TimeTickEvent>(OnTimeTick);
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
        EventBus.Unsubscribe<GiftGivenEvent>(OnGiftGiven);
        EventBus.Unsubscribe<TimeTickEvent>(OnTimeTick);
    }

    private void OnGiftGiven(GiftGivenEvent evt)
    {
        if (evt.NPC != definition) return;
        int points = definition.IsLikedGift(evt.Item) ? 2 : 1;
        friendshipPoints += points;
    }

    private void OnTimeTick(TimeTickEvent evt)
    {
        if (schedule == null || schedule.Length == 0) return;

        float hour = evt.NormalizedTime * 24f;

        // Only re-evaluate once per in-game hour to avoid thrashing
        int hourInt = Mathf.FloorToInt(hour);
        if (hourInt == Mathf.FloorToInt(_lastScheduleHour)) return;
        _lastScheduleHour = hour;

        // Find the active schedule entry (last entry whose startHour <= current hour)
        Transform[] best = scheduleWaypoints; // fallback
        for (int i = schedule.Length - 1; i >= 0; i--)
        {
            if (hour >= schedule[i].startHour && schedule[i].waypoints != null && schedule[i].waypoints.Length > 0)
            {
                best = schedule[i].waypoints;
                break;
            }
        }

        if (best != _activeWaypoints)
        {
            _activeWaypoints = best;
            currentWaypointIndex = 0;
        }
    }

    private void Update()
    {
        if (_activeWaypoints == null || _activeWaypoints.Length == 0) return;

        var target = _activeWaypoints[currentWaypointIndex];
        if (target == null) return;

        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        // Flip sprite to face movement direction
        if (spriteRenderer != null)
        {
            Vector2 dir = target.position - transform.position;
            if (Mathf.Abs(dir.x) > 0.01f)
                spriteRenderer.flipX = dir.x < 0;
        }

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
            currentWaypointIndex = (currentWaypointIndex + 1) % _activeWaypoints.Length;
    }

    public bool CanInteract(uint playerId)
    {
        return definition != null;
    }

    public string GetInteractionPrompt()
    {
        return definition != null ? $"Talk to {definition.npcName}" : "Talk";
    }

    public void Interact(uint playerId)
    {
        if (definition == null) return;

        // Face the player
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            if (spriteRenderer != null)
                spriteRenderer.flipX = dir.x < 0;
        }

        // Check if the player is holding a giftable item
        ItemDefinition giftItem = GetHeldGiftableItem();
        if (giftItem != null && definition.acceptsGifts)
        {
            StartGiftDialogue(playerId, giftItem);
            return;
        }

        // Try friendship-unlocked dialogue first (highest threshold that's met)
        DialogueTree activeDialogue = greetingDialogue;
        if (friendshipDialogues != null)
        {
            for (int i = friendshipDialogues.Length - 1; i >= 0; i--)
            {
                if (friendshipPoints >= friendshipDialogues[i].requiredPoints && friendshipDialogues[i].dialogue != null)
                {
                    activeDialogue = friendshipDialogues[i].dialogue;
                    break;
                }
            }
        }

        if (activeDialogue != null)
        {
            DialogueManager.Instance?.StartDialogue(activeDialogue, definition);
        }
        else if (definition.greetings != null && definition.greetings.Length > 0)
        {
            string greeting = definition.greetings[Random.Range(0, definition.greetings.Length)];
            Debug.Log($"[{definition.npcName}] {greeting}");
        }
    }

    private ItemDefinition GetHeldGiftableItem()
    {
        var quickbar = FindFirstObjectByType<QuickbarUI>();
        if (quickbar == null) return null;

        var slot = quickbar.GetActiveSlotData();
        if (slot == null || slot.IsEmpty()) return null;
        if (slot.item is ToolDefinition) return null;
        if (slot.item.category == ItemCategory.Seed) return null;

        return slot.item;
    }

    private void StartGiftDialogue(uint playerId, ItemDefinition giftItem)
    {
        var tree = ScriptableObject.CreateInstance<DialogueTree>();
        string response = definition.GetGiftResponse(giftItem);

        tree.nodes = new DialogueTree.DialogueNode[]
        {
            new DialogueTree.DialogueNode
            {
                speakerName = definition.npcName,
                text = $"Oh, is that a {giftItem.itemName}?",
                choiceTexts = new[] { $"Give {giftItem.itemName}", "Never mind" },
                choiceNextIndices = new[] { 1, -1 },
                nextIndex = -1
            },
            new DialogueTree.DialogueNode
            {
                speakerName = definition.npcName,
                text = response,
                nextIndex = -1
            }
        };

        DialogueManager.Instance.PendingChoiceCallback = (choiceIndex) =>
        {
            if (choiceIndex == 0)
            {
                InventoryManager.Instance.RemoveItem(giftItem, 1);
                EventBus.Publish(new GiftGivenEvent
                {
                    GiverId = playerId,
                    NPC = definition,
                    Item = giftItem
                });
            }
        };

        DialogueManager.Instance.StartDialogue(tree, definition);
    }

    #region ISaveable

    public string SaveState()
    {
        return JsonUtility.ToJson(new NPCSaveData { friendship = friendshipPoints });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<NPCSaveData>(json);
        if (data != null)
            friendshipPoints = data.friendship;
    }

    [System.Serializable]
    private class NPCSaveData
    {
        public int friendship;
    }

    #endregion
}

[System.Serializable]
public struct FriendshipDialogue
{
    [Tooltip("Friendship points required to unlock this dialogue")]
    public int requiredPoints;
    public DialogueTree dialogue;
}

[System.Serializable]
public struct NPCScheduleEntry
{
    [Tooltip("Hour (24h) when this schedule block begins. Entries should be ordered earliest-to-latest.")]
    [Range(0f, 24f)]
    public float startHour;

    [Tooltip("Waypoints the NPC patrols during this time block.")]
    public Transform[] waypoints;
}
