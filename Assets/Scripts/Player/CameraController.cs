using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float zOffset = -10f;
    [SerializeField] private bool clampToCloud = true;

    private CloudGenerator cloudGenerator;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            cam.orthographicSize = 8f;

        cloudGenerator = FindFirstObjectByType<CloudGenerator>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, zOffset);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        if (clampToCloud && cloudGenerator != null)
            smoothedPosition = ClampToCloudBounds(smoothedPosition);

        transform.position = smoothedPosition;
    }

    private Vector3 ClampToCloudBounds(Vector3 pos)
    {
        if (cam == null || cloudGenerator == null) return pos;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = halfWidth;
        float maxX = cloudGenerator.Width - halfWidth;
        float minY = halfHeight;
        float maxY = cloudGenerator.Height - halfHeight;

        if (minX > maxX) pos.x = cloudGenerator.Width / 2f;
        else pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (minY > maxY) pos.y = cloudGenerator.Height / 2f;
        else pos.y = Mathf.Clamp(pos.y, minY, maxY);

        pos.z = zOffset;
        return pos;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
