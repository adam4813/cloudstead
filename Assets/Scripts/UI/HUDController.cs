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

    private void Start()
    {
        EventBus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
        EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
        EventBus.Subscribe<DayStartedEvent>(OnDateStarted
        );

        // TODO (fix-hud-gold-init): initial gold is never shown because EconomyManager
        // publishes GoldChangedEvent before HUD subscribes. Read EconomyManager.Instance.PlayerGold
        // directly here once execution-order/init timing is sorted out.

        // Set defaults
        if (clockText != null) clockText.text = "Day 1 — Spring";
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
