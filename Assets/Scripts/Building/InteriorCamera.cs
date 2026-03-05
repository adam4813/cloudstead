using UnityEngine;

public class InteriorCamera : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;

    private void Start()
    {
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

        EventBus.Subscribe<InteriorEnteredEvent>(OnInteriorEntered);
        EventBus.Subscribe<InteriorExitedEvent>(OnInteriorExited);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<InteriorEnteredEvent>(OnInteriorEntered);
        EventBus.Unsubscribe<InteriorExitedEvent>(OnInteriorExited);
    }

    private void OnInteriorEntered(InteriorEnteredEvent evt)
    {
        if (cameraController == null) return;
        cameraController.SnapToTarget();
    }

    private void OnInteriorExited(InteriorExitedEvent evt)
    {
        if (cameraController == null) return;
        cameraController.SnapToTarget();
    }
}
