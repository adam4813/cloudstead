using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightController : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Gradient dayNightGradient;
    [SerializeField] private AnimationCurve intensityCurve;

    private void Start()
    {
        if (globalLight == null)
            globalLight = GetComponent<Light2D>();

        if (dayNightGradient == null || dayNightGradient.colorKeys.Length == 0)
            SetDefaultGradient();

        if (intensityCurve == null || intensityCurve.length == 0)
            SetDefaultIntensityCurve();
    }

    private void Update()
    {
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
                new(new Color(1f, 0.89f, 0.71f), 0f),       // 6AM warm gold
                new(new Color(1f, 1f, 0.94f), 0.25f),        // noon bright
                new(new Color(1f, 0.70f, 0.28f), 0.5f),      // 6PM amber
                new(new Color(0.48f, 0.41f, 0.68f), 0.65f),  // 8PM dusky purple
                new(new Color(0.10f, 0.10f, 0.24f), 0.75f),  // midnight deep blue
                new(new Color(0.29f, 0.33f, 0.41f), 0.95f),  // 5AM predawn
                new(new Color(1f, 0.89f, 0.71f), 1f),        // back to dawn
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
            new Keyframe(0f, 0.8f),      // dawn
            new Keyframe(0.25f, 1f),     // noon
            new Keyframe(0.5f, 0.7f),    // dusk
            new Keyframe(0.75f, 0.3f),   // midnight
            new Keyframe(1f, 0.8f)       // dawn again
        );
    }
}
