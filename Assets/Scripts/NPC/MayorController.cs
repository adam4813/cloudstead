using UnityEngine;

/// <summary>
/// Triggers the mayor arc cutscenes on the appropriate days.
///
/// Day 1: fires when FarmStartSetup publishes NewGameStartedEvent.
/// Day 2: flags ready on DayStartedEvent(day >= 2), then fires when the player
///         exits an interior (they wake up in the farmhouse on day 2). If already
///         outside when day 2 starts, fires immediately.
/// </summary>
public class MayorController : MonoBehaviour
{
    [SerializeField] private CutsceneDefinition day1Cutscene;
    [SerializeField] private CutsceneDefinition day2Cutscene;

    private bool _day2Ready;
    private bool _day2Triggered;

    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoadPending)
        {
            gameObject.SetActive(false);
            return;
        }

        EventBus.Subscribe<NewGameStartedEvent>(OnNewGameStarted);
        EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
        EventBus.Subscribe<InteriorExitedEvent>(OnInteriorExited);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<NewGameStartedEvent>(OnNewGameStarted);
        EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
        EventBus.Unsubscribe<InteriorExitedEvent>(OnInteriorExited);
    }

    private void OnNewGameStarted(NewGameStartedEvent evt)
    {
        if (day1Cutscene == null)
        {
            Debug.LogWarning("[MayorController] day1Cutscene is not assigned.");
            return;
        }
        CutscenePlayer.Instance?.Play(day1Cutscene);
    }

    private void OnDayStarted(DayStartedEvent evt)
    {
        if (evt.Day < 2 || _day2Triggered) return;
        _day2Ready = true;

        // Player may be outside already (e.g. they slept outside or loaded on day 2+)
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.CurrentInterior == null)
            TriggerDay2();
    }

    private void OnInteriorExited(InteriorExitedEvent evt)
    {
        if (!_day2Ready || _day2Triggered) return;
        TriggerDay2();
    }

    private void TriggerDay2()
    {
        _day2Triggered = true;
        gameObject.SetActive(true);

        if (day2Cutscene == null)
        {
            Debug.LogWarning("[MayorController] day2Cutscene is not assigned.");
            return;
        }
        CutscenePlayer.Instance?.Play(day2Cutscene);
    }
}

