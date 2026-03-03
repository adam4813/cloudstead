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

        var interiors = FindObjectsByType<BuildingInterior>(FindObjectsSortMode.None);
        foreach (var interior in interiors)
        {
            if (interior.BuildingId != evt.BuildingId) continue;
            var b = interior.GetBounds();
            if (b.size == Vector3.zero) continue;
            cameraController.SetOverrideBounds(b.min, b.max);
            cameraController.SnapToTarget();
            return;
        }
    }

    private void OnInteriorExited(InteriorExitedEvent evt)
    {
        if (cameraController == null) return;
        cameraController.ClearOverrideBounds();
        cameraController.SnapToTarget();
    }
}
