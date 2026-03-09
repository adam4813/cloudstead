using UnityEngine;

public class GameManager : Singleton<GameManager>, ISaveable
{
    [SerializeField] private GameState currentState = GameState.Playing;
    [SerializeField] private WorldSettings settings = new();

    private GameState previousState = GameState.Playing;

    public WorldSettings Settings => settings;

    // Convenience accessors
    public bool PauseTimeInMenus => settings.pauseTimeInMenus;
    public int PlayerInteractionRange => settings.playerInteractionRange;
    public float DayLengthSeconds => settings.dayLengthSeconds;

    public GameState CurrentState => currentState;
    public GameState PreviousState => previousState;
    public bool IsPlaying => currentState == GameState.Playing;

    public override void Initialize()
    {
        SaveManager.Instance?.Register(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SaveManager.Instance?.Unregister(this);
    }

    public void SetState(GameState newState)
    {
        if (newState == currentState) return;

        previousState = currentState;
        currentState = newState;

        EventBus.Publish(new GameStateChangedEvent
        {
            Previous = previousState,
            Current = newState
        });
    }

    /// <summary>
    /// Returns to the state that was active before the current one.
    /// Useful for overlay UIs (inventory, map) that should restore Airship/Playing/etc.
    /// </summary>
    public void RestorePreviousState()
    {
        SetState(previousState);
    }

    /// <summary>Applies new settings at runtime. Publishes event so systems can react.</summary>
    public void ApplySettings(WorldSettings newSettings)
    {
        settings = newSettings.Clone();
        EventBus.Publish(new WorldSettingsChangedEvent { Settings = settings });
    }

    #region ISaveable

    public string SaveState()
    {
        return JsonUtility.ToJson(settings);
    }

    public void RestoreState(string json)
    {
        var loaded = JsonUtility.FromJson<WorldSettings>(json);
        if (loaded != null)
        {
            settings = loaded;
            EventBus.Publish(new WorldSettingsChangedEvent { Settings = settings });
        }
    }

    #endregion
}
