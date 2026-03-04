using System.Collections;
using UnityEngine;

/// <summary>
/// Scripted 2-day onboarding sequence: "The Mayor's Welcome."
/// Day 1: Walking tour of farm → mayor departs.
/// Day 2: Mayor returns → flies player to town → town tour → airship ownership transfer.
/// </summary>
public class MayorController : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private NPCDefinition definition;
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Day 1 Tour")]
    [SerializeField] private Transform[] farmTourWaypoints;
    [SerializeField, TextArea(1, 3)] private string[] farmTourDialogueLines;

    [Header("Day 2 Tour")]
    [SerializeField] private Transform[] townTourWaypoints;
    [SerializeField, TextArea(1, 3)] private string[] townTourDialogueLines;

    [Header("References")]
    [Tooltip("The mayor's airship (used for Day 1 departure and Day 2 travel)")]
    [SerializeField] private AirshipController mayorAirship;

    [Tooltip("The player's airship at the town shipyard (ownership transferred on Day 2)")]
    [SerializeField] private AirshipController playerAirship;

    [Tooltip("Player's ownerId — transferred to the airship on Day 2")]
    [SerializeField] private uint playerOwnerId = 0;

    [Header("Dialogue")]
    [SerializeField, TextArea(1, 3)] private string arrivalLine = "Welcome to your new home in the clouds!";
    [SerializeField, TextArea(1, 3)] private string departureLine = "I'll swing back tomorrow morning. Get some rest.";
    [SerializeField, TextArea(1, 3)] private string day2ArrivalLine = "Morning! Ready to see the town?";
    [SerializeField, TextArea(1, 3)] private string shipyardLine = "Your airship was being finished up here. She's all yours now. Fly safe.";

    private SpriteRenderer _spriteRenderer;
    private bool _day1Complete;
    private bool _day2Complete;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        var settings = GameManager.Instance?.Settings;
        if (settings == null || !settings.isNewGame)
        {
            gameObject.SetActive(false);
            return;
        }

        EventBus.Subscribe<DayStartedEvent>(OnDayStarted);

        // Day 1 starts immediately on new game
        StartCoroutine(Day1Sequence());
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
    }

    private void OnDayStarted(DayStartedEvent evt)
    {
        if (evt.Day >= 2 && !_day2Complete && _day1Complete)
        {
            StartCoroutine(Day2Sequence());
        }
    }

    private IEnumerator Day1Sequence()
    {
        yield return new WaitForSeconds(0.5f);

        // Arrival dialogue
        yield return ShowDialogue(arrivalLine);

        // Walk tour
        if (farmTourWaypoints != null)
        {
            for (int i = 0; i < farmTourWaypoints.Length; i++)
            {
                yield return WalkTo(farmTourWaypoints[i].position);

                if (farmTourDialogueLines != null && i < farmTourDialogueLines.Length)
                    yield return ShowDialogue(farmTourDialogueLines[i]);
            }
        }

        // Departure
        yield return ShowDialogue(departureLine);

        _day1Complete = true;

        // Mayor disappears (boards his ship and leaves)
        gameObject.SetActive(false);
    }

    private IEnumerator Day2Sequence()
    {
        gameObject.SetActive(true);

        // Mayor arrives at the player's farm
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            transform.position = player.transform.position + Vector3.right * 2f;
        }

        yield return new WaitForSeconds(0.5f);
        yield return ShowDialogue(day2ArrivalLine);

        // Town tour
        if (townTourWaypoints != null)
        {
            for (int i = 0; i < townTourWaypoints.Length; i++)
            {
                yield return WalkTo(townTourWaypoints[i].position);

                if (townTourDialogueLines != null && i < townTourDialogueLines.Length)
                    yield return ShowDialogue(townTourDialogueLines[i]);
            }
        }

        // Transfer airship ownership
        yield return ShowDialogue(shipyardLine);

        if (playerAirship != null)
        {
            playerAirship.OwnerId = playerOwnerId;
        }

        _day2Complete = true;

        // Clear isNewGame flag
        if (GameManager.Instance != null)
        {
            var settings = GameManager.Instance.Settings.Clone();
            settings.isNewGame = false;
            GameManager.Instance.ApplySettings(settings);
        }

        // Mayor departs for good
        gameObject.SetActive(false);
    }

    private IEnumerator WalkTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            Vector3 dir = (target - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;

            if (_spriteRenderer != null)
                _spriteRenderer.flipX = dir.x < 0;

            yield return null;
        }
    }

    private IEnumerator ShowDialogue(string line)
    {
        if (string.IsNullOrEmpty(line)) yield break;
        if (DialogueManager.Instance == null || definition == null) yield break;

        var tree = ScriptableObject.CreateInstance<DialogueTree>();
        tree.nodes = new DialogueTree.DialogueNode[]
        {
            new DialogueTree.DialogueNode
            {
                speakerName = definition.npcName,
                text = line,
                nextIndex = -1
            }
        };

        DialogueManager.Instance.StartDialogue(tree, definition);

        // Wait for dialogue to finish
        yield return new WaitUntil(() =>
            GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameState.Dialogue);
    }
}
