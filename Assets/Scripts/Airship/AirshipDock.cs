using UnityEngine;

/// <summary>
/// The airship dock — a fixed point on the cloud edge where the airship is parked.
/// Implements IInteractable so the player can board by pressing E.
///
/// Must have a LandingPad component on the same GameObject so the airship can
/// also return to this dock (AirshipController.OnLand checks for AirshipDock
/// on the nearest LandingPad to decide whether to re-moor the airship).
///
/// Future: AirshipDock should implement IBuildable and be placeable/moveable
/// on edge tiles when the Item Placement gym is implemented.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(LandingPad))]
public class AirshipDock : MonoBehaviour, IInteractable
{
    [SerializeField] private AirshipController dockedAirship;

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    // ── IInteractable ────────────────────────────────────────────────────────

    public void Interact(uint playerId)
    {
        if (dockedAirship == null)
        {
            Debug.LogWarning("[AirshipDock] No AirshipController assigned.");
            return;
        }

        if (dockedAirship.HasBoardedPlayer)
            return; // already occupied

        PlayerController player = FindBoardingPlayer(playerId);
        if (player == null) return;

        // Switch to Airship action map BEFORE BoardPlayer so input is ready
        GameManager.Instance.SetState(GameState.Airship);
        dockedAirship.BoardPlayer(player);
    }

    public string GetInteractionPrompt() => "Board Airship (E)";

    public bool CanInteract(uint playerId)
    {
        return dockedAirship != null
            && !dockedAirship.HasBoardedPlayer
            && GameManager.Instance.IsPlaying;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private PlayerController FindBoardingPlayer(uint playerId)
    {
        // Multiplayer-ready: match by ownerId.
        // Solo fallback: return the only PlayerController in the scene.
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
            if (p.ownerId == playerId) return p;

        return players.Length > 0 ? players[0] : null;
    }
}
