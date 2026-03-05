using UnityEngine;

/// <summary>
/// Follows a target with smooth movement and cloud-boundary clamping.
/// When the player boards an airship (AirshipBoardedEvent), switches
/// to follow the airship with a wider zoom to show the open sky.
/// When the player lands (AirshipLandedEvent), switches back to the
/// player with the normal zoom.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float zOffset = -10f;

    [Header("Zoom")]
    [SerializeField] private float onCloudZoom = 8f;
    [SerializeField] private float airshipZoom = 14f;
    [SerializeField] private float zoomLerpSpeed = 2f;

    [Header("Bounds")]
    [SerializeField] private bool clampToCloud = true;

    private Camera cam;
    private Transform currentTarget;
    private float targetZoom;

    private bool _hasOverrideBounds;
    private Vector3 _overrideMin;
    private Vector3 _overrideMax;

    private void Awake()
    {
        currentTarget = target;
    }

    private void Start()
    {
        cam = GetComponent<Camera>();
        targetZoom = onCloudZoom;
        if (cam != null)
            cam.orthographicSize = onCloudZoom;

        EventBus.Subscribe<AirshipBoardedEvent>(OnAirshipBoarded);
        EventBus.Subscribe<AirshipLandedEvent>(OnAirshipLanded);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<AirshipBoardedEvent>(OnAirshipBoarded);
        EventBus.Unsubscribe<AirshipLandedEvent>(OnAirshipLanded);
    }

    private void LateUpdate()
    {
        if (currentTarget == null) return;

        // Smooth follow
        Vector3 desired = new Vector3(currentTarget.position.x, currentTarget.position.y, zOffset);
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        // Clamp only when on the cloud (not during airship flight)
        if (clampToCloud && CloudIsland.Current != null)
            smoothed = ClampToCloudBounds(smoothed);

        transform.position = smoothed;

        // Smooth zoom
        if (cam != null)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomLerpSpeed * Time.deltaTime);
    }

    private Vector3 ClampToCloudBounds(Vector3 pos)
    {
        if (_hasOverrideBounds)
        {
            pos.x = Mathf.Clamp(pos.x, _overrideMin.x, _overrideMax.x);
            pos.y = Mathf.Clamp(pos.y, _overrideMin.y, _overrideMax.y);
            pos.z = zOffset;
            return pos;
        }

        var cg = CloudIsland.Current;
        if (cam == null || cg == null) return pos;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float ox = cg.TileOrigin.x;
        float oy = cg.TileOrigin.y;

        float minX = ox + halfWidth;
        float maxX = ox + cg.Width - halfWidth;
        float minY = oy + halfHeight;
        float maxY = oy + cg.Height - halfHeight;

        if (minX > maxX) pos.x = ox + cg.Width / 2f;
        else pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (minY > maxY) pos.y = oy + cg.Height / 2f;
        else pos.y = Mathf.Clamp(pos.y, minY, maxY);

        pos.z = zOffset;
        return pos;
    }

    public void SetOverrideBounds(Vector3 min, Vector3 max)
    {
        _overrideMin = min;
        _overrideMax = max;
        _hasOverrideBounds = true;
        clampToCloud = true;
    }

    public void ClearOverrideBounds()
    {
        _hasOverrideBounds = false;
    }

    /// <summary>Instantly moves the camera to the target, skipping smooth lerp.</summary>
    public void SnapToTarget()
    {
        if (currentTarget == null) return;
        transform.position = new Vector3(currentTarget.position.x, currentTarget.position.y, zOffset);
    }

    private void OnAirshipBoarded(AirshipBoardedEvent evt)
    {
        if (evt.AirshipTransform != null)
            currentTarget = evt.AirshipTransform;
        targetZoom = airshipZoom;
        clampToCloud = false;
    }

    private void OnAirshipLanded(AirshipLandedEvent evt)
    {
        currentTarget = target; // always the player transform
        targetZoom = onCloudZoom;
        clampToCloud = true;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        currentTarget = newTarget;
    }
}
