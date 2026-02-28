using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    [SerializeField] private NPCDefinition definition;
    [SerializeField] private Transform[] scheduleWaypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private DialogueTree greetingDialogue;

    private SpriteRenderer spriteRenderer;
    private int currentWaypointIndex;

    // Multiplayer readiness — will be assigned when networking is added
    [System.NonSerialized] public uint ownerId;

    public NPCDefinition Definition => definition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (scheduleWaypoints == null || scheduleWaypoints.Length == 0) return;

        var target = scheduleWaypoints[currentWaypointIndex];
        if (target == null) return;

        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
            currentWaypointIndex = (currentWaypointIndex + 1) % scheduleWaypoints.Length;
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

        if (definition.isMerchant)
        {
            ShopManager.Instance?.OpenShop(definition);
        }
        else if (greetingDialogue != null)
        {
            DialogueManager.Instance?.StartDialogue(greetingDialogue, definition);
        }
        else if (definition.greetings != null && definition.greetings.Length > 0)
        {
            string greeting = definition.greetings[Random.Range(0, definition.greetings.Length)];
            Debug.Log($"[{definition.npcName}] {greeting}");
        }
    }
}
