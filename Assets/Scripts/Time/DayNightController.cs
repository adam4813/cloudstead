using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightController : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Gradient dayNightGradient;
    [SerializeField] private AnimationCurve intensityCurve;

    private bool _isInsideInterior;

    private void Start()
    {
        if (globalLight == null)
            globalLight = GetComponent<Light2D>();

        //if (dayNightGradient == null || dayNightGradient.colorKeys.Length == 0)
            SetDefaultGradient();

        //if (intensityCurve == null || intensityCurve.length == 0)
            SetDefaultIntensityCurve();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<InteriorEnteredEvent>(OnInteriorEntered);
        EventBus.Subscribe<InteriorExitedEvent>(OnInteriorExited);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InteriorEnteredEvent>(OnInteriorEntered);
        EventBus.Unsubscribe<InteriorExitedEvent>(OnInteriorExited);
    }

    private void OnInteriorEntered(InteriorEnteredEvent evt)
    {
        _isInsideInterior = true;
        if (globalLight != null) globalLight.enabled = false;
    }

    private void OnInteriorExited(InteriorExitedEvent evt)
    {
        _isInsideInterior = false;
        if (globalLight != null) globalLight.enabled = true;
    }

    private void Update()
    {
        if (_isInsideInterior) return;
        if (TimeManager.Instance == null || globalLight == null) return;

        float t = TimeManager.Instance.CurrentTime;
        globalLight.color = dayNightGradient.Evaluate(t);
        globalLight.intensity = intensityCurve.Evaluate(t);
    }

    private void SetDefaultGradient()
    {
        // Warm gold dawn → bright noon → amber dusk → deep blue night
        dayNightGradient = new Gradient();
        dayNightGradient.SetKeys(
            new GradientColorKey[]
            {
                new(new Color(0.10f, 0.10f, 0.24f), 0.0f),  // midnight deep blue
                new(new Color(0.29f, 0.33f, 0.41f), 0.075f),  // 5AM predawn
                new(new Color(1f, 0.89f, 0.71f), 0.15f),       // 6AM warm gold
                new(new Color(1f, 1f, 0.94f), 0.25f),        // morning bright
                new(new Color(1f, 1f, 0.94f), 0.65f),        // noon bright
                new(new Color(1f, 0.70f, 0.28f), 0.75f),      // 6PM amber
                new(new Color(0.48f, 0.41f, 0.68f), 0.85f),  // 8PM dusky purple
                new(new Color(0.10f, 0.10f, 0.24f), 1f),  // midnight deep blue
            },
            new GradientAlphaKey[]
            {
                new(1f, 0f),
                new(1f, 1f)
            }
        );
    }

    private void SetDefaultIntensityCurve()
    {
        intensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.4f),      // dawn
            new Keyframe(0.075f, 0.5f),      // dawn
            new Keyframe(0.15f, 0.7f),      // morning
            new Keyframe(0.25f, 1f),     // morning
            new Keyframe(0.65f, 1f),     // noon
            new Keyframe(0.75f, 0.7f),    // dusk
            new Keyframe(0.85f, 0.6f),   // midnight
            new Keyframe(1f, 0.4f)
        );
    }
}
