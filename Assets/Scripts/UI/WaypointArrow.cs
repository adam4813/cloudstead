using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-edge arrow that points toward the active waypoint.
/// When the target is off-screen, the arrow sits at the screen edge.
/// When on-screen, it hovers above the target position.
/// Shows distance in a child TMP label.
/// </summary>
public class WaypointArrow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private Image arrowImage;
    [SerializeField] private TextMeshProUGUI distanceLabel;

    [Header("Settings")]
    [SerializeField] private float edgePadding = 60f;
    [SerializeField] private Color arrowColor = new(1f, 0.85f, 0.4f, 0.9f);

    private Camera _cam;
    private bool _hasWaypoint;
    private IslandInfo _target;

    private void OnEnable()
    {
        EventBus.Subscribe<WaypointSetEvent>(OnWaypointSet);
        EventBus.Subscribe<WaypointClearedEvent>(OnWaypointCleared);

        // Pick up existing waypoint if we enable late
        if (WaypointManager.Instance != null && WaypointManager.Instance.HasWaypoint)
        {
            _target = WaypointManager.Instance.CurrentWaypoint.Value;
            _hasWaypoint = true;
        }
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<WaypointSetEvent>(OnWaypointSet);
        EventBus.Unsubscribe<WaypointClearedEvent>(OnWaypointCleared);
    }

    private void OnWaypointSet(WaypointSetEvent evt)
    {
        _target = evt.Target;
        _hasWaypoint = true;
    }

    private void OnWaypointCleared(WaypointClearedEvent evt)
    {
        _hasWaypoint = false;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (!_hasWaypoint)
        {
            SetVisible(false);
            return;
        }

        // Only show during Airship state
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameState.Airship)
        {
            SetVisible(false);
            return;
        }

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) { SetVisible(false); return; }

        SetVisible(true);

        Vector3 worldTarget = _target.worldCenter;
        Vector3 playerPos = _cam.transform.position;
        float distance = Vector2.Distance(playerPos, worldTarget);

        // Update distance label
        if (distanceLabel != null)
            distanceLabel.text = distance < 10f ? $"{distance:F1}m" : $"{Mathf.RoundToInt(distance)}m";

        // Convert target to viewport space
        Vector3 viewportPos = _cam.WorldToViewportPoint(worldTarget);
        bool isOnScreen = viewportPos.x > 0.05f && viewportPos.x < 0.95f &&
                          viewportPos.y > 0.05f && viewportPos.y < 0.95f &&
                          viewportPos.z > 0f;

        if (isOnScreen)
        {
            // Place arrow above target on screen
            Vector2 screenPos = _cam.WorldToScreenPoint(worldTarget);
            arrowRect.position = screenPos + Vector2.up * 40f;
            arrowRect.rotation = Quaternion.Euler(0, 0, 0); // point up
        }
        else
        {
            // Clamp to screen edge
            Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 screenTarget = _cam.WorldToScreenPoint(worldTarget);

            // Handle behind-camera case
            if (viewportPos.z < 0f)
                screenTarget = screenCenter - (screenTarget - screenCenter);

            Vector2 dir = (screenTarget - screenCenter).normalized;

            // Clamp to screen bounds with padding
            float halfW = Screen.width * 0.5f - edgePadding;
            float halfH = Screen.height * 0.5f - edgePadding;

            // Find the intersection with the screen edge rectangle
            float tX = Mathf.Abs(dir.x) > 0.001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue;
            float tY = Mathf.Abs(dir.y) > 0.001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue;
            float t = Mathf.Min(tX, tY);

            Vector2 edgePos = screenCenter + dir * t;
            arrowRect.position = edgePos;

            // Rotate arrow to point toward target
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            arrowRect.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (arrowImage != null)
            arrowImage.color = arrowColor;
    }

    private void SetVisible(bool visible)
    {
        if (arrowImage != null) arrowImage.enabled = visible;
        if (distanceLabel != null) distanceLabel.enabled = visible;
    }
}
