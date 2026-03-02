using UnityEngine;

/// <summary>
/// Marks the fixed mooring point on the cloud edge where the airship parks.
/// Interacting with the dock teleports the player onto the airship deck so
/// they can walk to the helm and press E to start flying.
///
/// Requires a LandingPad so AirshipController.OnLand() can detect this as
/// a dock (re-moors to Kinematic) vs an open-sky pad (stays Dynamic).
///
/// Future: implement IBuildable so the dock can be placed/moved on edge tiles,
/// and replace the teleport with a walkable ramp.
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

        // Teleport player onto the airship deck — no game state change.
        // Player walks to the helm child GO and presses E to start flying.
        Vector3 destination = boardingPosition != null
            ? boardingPosition.position
            : dockedAirship.transform.position;

        player.transform.position = destination;

        // Disable camera cloud-clamping immediately so it follows the player
        // onto the ship. When AirshipController.BoardPlayer fires later it
        // publishes this event again — camera is already there, no jump.
        EventBus.Publish(new AirshipBoardedEvent { AirshipTransform = dockedAirship.transform });
    }

    public string GetInteractionPrompt() => "Board Airship (E)";

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

