using UnityEngine;

/// <summary>
/// Marks the fixed mooring point on the cloud edge where the airship parks.
/// Interacting with the dock teleports the player onto the airship deck so
/// they can walk to the helm and press E to start flying.
///
/// When the player is already aboard a docked airship, interacting with
/// the dock enters ship edit mode (tile editing via AirshipBuildMode).
///
/// Requires a LandingPad so AirshipController.OnLand() can detect this as
/// a dock (re-moors to Kinematic) vs an open-sky pad (stays Dynamic).
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(LandingPad))]
public class AirshipDock : MonoBehaviour, IInteractable
{
    [SerializeField] private AirshipController dockedAirship;
    [SerializeField] private Transform boardingPosition;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // ── IInteractable ────────────────────────────────────────────────────────

    public void Interact(uint playerId)
    {
        if (dockedAirship == null)
        {
            Debug.LogWarning("[AirshipDock] No AirshipController assigned.");
            return;
        }

        if (dockedAirship.IsFlying) return;

        PlayerController player = FindPlayer(playerId);
        if (player == null) return;

        if (player.CurrentAirship == dockedAirship)
        {
            // Already aboard this airship — enter tile edit mode
            var buildMode = dockedAirship.GetComponent<AirshipBuildMode>();
            if (buildMode != null && !buildMode.IsActive)
                buildMode.EnterEditMode();
        }
        else
        {
            // Board the airship from the cloud
            Vector3 destination = boardingPosition != null
                ? boardingPosition.position
                : dockedAirship.transform.position;

            player.transform.position = destination;
            player.CurrentAirship = dockedAirship;
            player.CurrentCloud = null;

            // Set airship placement context so normal item placement works on deck
            PlacementManager.Instance?.SetContext(
                new AirshipPlacementContext(dockedAirship.transform,
                    dockedAirship.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>()));

            EventBus.Publish(new AirshipBoardedEvent { AirshipTransform = dockedAirship.transform });
        }
    }

    public string GetInteractionPrompt()
    {
        var player = FindPlayer(0);
        if (player != null && player.CurrentAirship == dockedAirship)
            return "Edit Ship (E)";
        return "Board Airship (E)";
    }

    public bool CanInteract(uint playerId) =>
        dockedAirship != null && !dockedAirship.IsFlying && GameManager.Instance.IsPlaying;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private PlayerController FindPlayer(uint playerId)
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
            if (p.ownerId == playerId) return p;
        return players.Length > 0 ? players[0] : null;
    }
}

