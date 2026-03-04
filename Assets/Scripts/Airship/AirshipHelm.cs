using UnityEngine;

/// <summary>
/// Placed on the helm child GameObject of the airship prefab.
/// The player walks to the helm and presses E to start piloting.
/// This separates "being on the airship" from "actively flying it",
/// leaving room for the player to walk around the deck in the future.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AirshipHelm : MonoBehaviour, IInteractable
{
    [SerializeField] private AirshipController airship;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (airship == null)
            airship = GetComponentInParent<AirshipController>();

        // Player-placed helms override the default prefab helm position
        if (airship != null && GetComponent<PlacedItem>() != null)
            airship.SetHelmPosition(transform);
    }

    private void OnDestroy()
    {
        // Revert to default helm when the placed helm is removed
        if (airship != null && GetComponent<PlacedItem>() != null)
            airship.SetHelmPosition(null);
    }

    // ── IInteractable ────────────────────────────────────────────────────────

    public void Interact(uint playerId)
    {
        if (airship == null) return;

        PlayerController player = FindPlayer(playerId);
        if (player == null) return;

        GameManager.Instance.SetState(GameState.Airship);
        airship.BoardPlayer(player);
    }

    public string GetInteractionPrompt() => "Pilot Airship (E)";

    public bool CanInteract(uint playerId) =>
        airship != null && !airship.IsFlying && GameManager.Instance.IsPlaying;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private PlayerController FindPlayer(uint playerId)
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
            if (p.ownerId == playerId) return p;
        return players.Length > 0 ? players[0] : null;
    }
}
