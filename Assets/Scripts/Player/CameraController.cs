using UnityEngine;

/// <summary>
/// Follows a target with smooth movement.
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

    private Camera cam;
    private Transform currentTarget;
    private float targetZoom;

    private void Awake()
    {
        currentTarget = target;
    }

    private void Start()
    {
        cam = GetComponentInChildren<Camera>();
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

        Vector3 desired = new Vector3(currentTarget.position.x, currentTarget.position.y, zOffset);
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        if (cam != null)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomLerpSpeed * Time.deltaTime);
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
    }

    private void OnAirshipLanded(AirshipLandedEvent evt)
    {
        currentTarget = target;
        targetZoom = onCloudZoom;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        currentTarget = newTarget;
    }
}
