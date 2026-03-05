using UnityEngine;

/// <summary>
/// Fires a CutsceneDefinition when a condition is met.
/// Conditions: OnNewGame, OnDayN, OnZoneEnter, OnInteract.
///
/// When playOnce is true the fired state is persisted through SaveManager
/// so the cutscene never replays after a save/load.
/// </summary>
public class CutsceneTrigger : MonoBehaviour, ISaveable
{
    public enum TriggerCondition { OnNewGame, OnDayN, OnZoneEnter, OnInteract }

    [Header("Cutscene")]
    [SerializeField] private CutsceneDefinition cutscene;

    [Header("Trigger")]
    [SerializeField] private TriggerCondition condition = TriggerCondition.OnNewGame;
    [SerializeField, Tooltip("Day number for OnDayN condition.")]
    private int targetDay = 2;
    [SerializeField, Tooltip("Player tag required to enter zone trigger. Leave blank for any collider.")]
    private string zoneTriggerTag = "Player";

    [Header("Playback")]
    [SerializeField, Tooltip("If true this trigger never fires more than once, even across save/load.")]
    private bool playOnce = true;
    [SerializeField, Tooltip("ISaveable key. Must be unique per scene. Auto-filled from GO name if blank.")]
    private string saveKey;

    private bool _hasFired;

    private void Awake()
    {
        if (string.IsNullOrEmpty(saveKey))
            saveKey = $"CutsceneTrigger:{gameObject.name}";
    }

    private void Start()
    {
        SaveManager.Instance?.Register(this);

        if (_hasFired && playOnce) return;

        switch (condition)
        {
            case TriggerCondition.OnNewGame:
                EventBus.Subscribe<NewGameStartedEvent>(OnNewGameStarted);
                break;
            case TriggerCondition.OnDayN:
                EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
                break;
        }
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
        EventBus.Unsubscribe<NewGameStartedEvent>(OnNewGameStarted);
        EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
    }

    // ── Condition Handlers ──────────────────────────────────────────────────

    private void OnNewGameStarted(NewGameStartedEvent evt) => Fire();

    private void OnDayStarted(DayStartedEvent evt)
    {
        if (evt.Day == targetDay)
            Fire();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (condition != TriggerCondition.OnZoneEnter) return;
        if (!string.IsNullOrEmpty(zoneTriggerTag) && !other.CompareTag(zoneTriggerTag)) return;
        Fire();
    }

    // Called by PlayerInteraction when this object is the interact target
    public void InteractFire()
    {
        if (condition != TriggerCondition.OnInteract) return;
        Fire();
    }

    // ── Core ────────────────────────────────────────────────────────────────

    private void Fire()
    {
        if (_hasFired && playOnce) return;
        if (CutscenePlayer.Instance == null || cutscene == null) return;

        _hasFired = true;
        CutscenePlayer.Instance.Play(cutscene);
    }

    // ── ISaveable ───────────────────────────────────────────────────────────

    string ISaveable.SaveKey => saveKey;

    public string SaveState()
    {
        return JsonUtility.ToJson(new SaveData { hasFired = _hasFired });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<SaveData>(json);
        if (data != null) _hasFired = data.hasFired;
    }

    [System.Serializable]
    private class SaveData { public bool hasFired; }
}
