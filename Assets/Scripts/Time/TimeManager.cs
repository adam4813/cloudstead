using UnityEngine;

public class TimeManager : Singleton<TimeManager>
{
    [SerializeField] private float dayLengthSeconds = 720f;

    private float currentTime; // 0-1 normalized (0=midnight, 0.25=6AM, 0.5=noon, 1=midnight)
    private int currentDay = 1;
    private Season currentSeason = Season.Spring;
    private int currentYear = 1;
    private bool isPaused;

    public float CurrentTime => currentTime;
    public int CurrentDay => currentDay;
    public Season CurrentSeason => currentSeason;
    public int CurrentYear => currentYear;
    public bool IsPaused { get => isPaused; set => isPaused = value; }

    public override void Initialize()
    {
        currentTime = 0.25f;
        EventBus.Publish(new TimeTickEvent { NormalizedTime = 0.25f });
        EventBus.Publish(new DayStartedEvent { Day = currentDay, Season = currentSeason });
    }

    private void Update()
    {
        if (isPaused) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        currentTime += Time.deltaTime / dayLengthSeconds;

        EventBus.Publish(new TimeTickEvent { NormalizedTime = currentTime });

        if (currentTime >= 1f)
        {
            currentTime = 0f;
            AdvanceDay();
        }
    }

    public void AdvanceDay()
    {
        EventBus.Publish(new DayEndedEvent { Day = currentDay, Season = currentSeason });

        currentDay++;
        if (currentDay > Constants.DAYS_PER_SEASON)
        {
            currentDay = 1;
            AdvanceSeason();
        }

        EventBus.Publish(new DayStartedEvent { Day = currentDay, Season = currentSeason });
    }

    private void AdvanceSeason()
    {
        var prev = currentSeason;
        currentSeason = currentSeason switch
        {
            Season.Spring => Season.Summer,
            Season.Summer => Season.Autumn,
            Season.Autumn => Season.Winter,
            Season.Winter => Season.Spring,
            _ => Season.Spring
        };

        if (currentSeason == Season.Spring)
            currentYear++;

        EventBus.Publish(new SeasonChangedEvent { NewSeason = currentSeason });
    }

    public void Sleep()
    {
        GameManager.Instance?.SetState(GameState.Sleeping);
        currentTime = 0.25f;
        EventBus.Publish(new TimeTickEvent { NormalizedTime = 0.25f });
        AdvanceDay();
        GameManager.Instance?.SetState(GameState.Playing);
    }

    public void PauseTime() => isPaused = true;
    public void ResumeTime() => isPaused = false;

    /// <summary>Returns hours in 24h format (6 = 6AM, 0 = midnight).</summary>
    public float GetCurrentHour()
    {
        return currentTime * 24f;
    }
}
