using UnityEngine;
using TMPro;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private UnityEngine.UI.Image staminaBarFill;
    [SerializeField] private Color normalStaminaColor = Color.green;
    [SerializeField] private Color lowStaminaColor = new Color(1f, 0.6f, 0f);

    private Coroutine pulseCoroutine;
    private int _currentDay = 1;
    private Season _currentSeason = Season.Spring;

    private void Start()
    {
        EventBus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
        EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
        EventBus.Subscribe<DayStartedEvent>(OnDateStarted);
        EventBus.Subscribe<TimeTickEvent>(OnTimeTick);

        // Show starting gold immediately (EconomyManager.Initialize runs before HUD subscribes)
        if (EconomyManager.Instance != null)
            OnGoldChanged(new GoldChangedEvent { NewAmount = EconomyManager.Instance.PlayerGold });

        // Set defaults
        if (clockText != null) clockText.text = FormatClockText(6f, _currentDay, _currentSeason);
        if (currencyText != null) currencyText.text = "0g";
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = 1f;
            staminaBarFill.color = normalStaminaColor;
        }
    }


    private void OnDestroy()
    {
        EventBus.Unsubscribe<DayStartedEvent>(OnDateStarted);
        EventBus.Unsubscribe<TimeTickEvent>(OnTimeTick);
        EventBus.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);
        EventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
    }

    public void UpdateClock(int day, Season season)
    {
        _currentDay = day;
        _currentSeason = season;
        RenderClock();
    }

    private int _lastDisplayedMinute = -1;

    private void OnTimeTick(TimeTickEvent evt)
    {
        // Only re-render when the displayed minute changes (avoids per-frame string allocation)
        float hour = TimeManager.Instance != null ? TimeManager.Instance.GetCurrentHour() : 6f;
        int totalMinutes = Mathf.FloorToInt(hour * 60f);
        if (totalMinutes != _lastDisplayedMinute)
        {
            _lastDisplayedMinute = totalMinutes;
            RenderClock();
        }
    }

    private void RenderClock()
    {
        if (clockText == null) return;
        float hour = TimeManager.Instance != null ? TimeManager.Instance.GetCurrentHour() : 6f;
        clockText.text = FormatClockText(hour, _currentDay, _currentSeason);
    }

    private static string FormatClockText(float hour24, int day, Season season)
    {
        int totalMinutes = Mathf.FloorToInt(hour24 * 60f);
        int minutes = totalMinutes % 60;
        int hours = totalMinutes / 60;
        string period = hours < 12 ? "AM" : "PM";
        int display = hours % 12;
        if (display == 0) display = 12;
        return $"{display}:{minutes:D2} {period} \u2014 Day {day}, {season}";
    }

    private void OnStaminaChanged(StaminaChangedEvent evt)
    {
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = evt.Current / evt.Max;

            if (evt.Current / evt.Max < 0.2f)
            {
                staminaBarFill.color = lowStaminaColor;
                if (pulseCoroutine == null)
                    pulseCoroutine = StartCoroutine(PulseStaminaBar());
            }
            else
            {
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                    pulseCoroutine = null;
                }
                staminaBarFill.color = normalStaminaColor;
            }
        }
    }

    private IEnumerator PulseStaminaBar()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f; // 0..1
            float alpha = Mathf.Lerp(0.5f, 1f, t);
            var c = lowStaminaColor;
            c.a = alpha;
            staminaBarFill.color = c;
            yield return null;
        }
    }

    private void OnGoldChanged(GoldChangedEvent evt)
    {
        if (currencyText != null)
            currencyText.text = $"{evt.NewAmount}g";
    }

    private void OnDateStarted(DayStartedEvent evt)
    {
        UpdateClock(evt.Day, evt.Season);
    }
}
