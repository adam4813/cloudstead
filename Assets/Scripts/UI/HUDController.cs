using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private UnityEngine.UI.Image staminaBarFill;

    private void Start()
    {
        EventBus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
        EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
        EventBus.Subscribe<DayStartedEvent>(OnDateStarted
        );

        // Set defaults
        if (clockText != null) clockText.text = "Day 1 — Spring";
        if (currencyText != null) currencyText.text = "0g";
        if (staminaBarFill != null) staminaBarFill.fillAmount = 1f;
    }


    private void OnDestroy()
    {
        EventBus.Unsubscribe<DayStartedEvent>(OnDateStarted);
        EventBus.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);
        EventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
    }

    public void UpdateClock(int day, Season season)
    {
        if (clockText != null)
            clockText.text = $"Day {day} — {season}";
    }

    private void OnStaminaChanged(StaminaChangedEvent evt)
    {
        if (staminaBarFill != null)
            staminaBarFill.fillAmount = evt.Current / evt.Max;
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
