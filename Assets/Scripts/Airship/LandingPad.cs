using UnityEngine;

/// <summary>
/// A landing destination in the sky. The airship lands here when the player
/// presses E (Airship/Land action) and is within landingRadius.
///
/// NOT IInteractable — landing is triggered via the Airship input map in
/// AirshipController, not by the player walking up to it on foot.
///
/// AirshipDock also uses this component (as a RequireComponent sibling) so
/// the home dock is both boardable and landable.
///
/// LandingPads self-register with SkyWorldManager in Start().
/// </summary>
public class LandingPad : MonoBehaviour
{
    [SerializeField] private string padName = "Landing Pad";
    [SerializeField] private float landingRadius = 4f;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private GameObject labelObject;

    private AirshipController trackedAirship;

    private void Start()
    {
        SkyWorldManager.Instance?.RegisterPad(this);
        if (labelObject != null)
            labelObject.SetActive(false);
    }

    private void OnDestroy()
    {
        SkyWorldManager.Instance?.UnregisterPad(this);
    }

    private void Update()
    {
        if (trackedAirship == null)
            trackedAirship = FindFirstObjectByType<AirshipController>();

        if (labelObject != null && trackedAirship != null)
        {
            float dist = Vector2.Distance(trackedAirship.transform.position, transform.position);
            bool show = dist <= landingRadius * 2.5f;
            if (labelObject.activeSelf != show)
                labelObject.SetActive(show);
        }
    }

    public bool CanLand(Vector2 airshipPos)
        => Vector2.Distance(airshipPos, transform.position) <= landingRadius;

    public Vector2 GetPlayerSpawnPosition()
        => playerSpawnPoint != null ? (Vector2)playerSpawnPoint.position : (Vector2)transform.position;

    public string GetPadName() => padName;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new UnityEngine.Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, landingRadius);
        Gizmos.color = new UnityEngine.Color(0.3f, 0.8f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, landingRadius * 2.5f);
    }
#endif
}
