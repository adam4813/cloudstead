using UnityEngine;

public class Bed : MonoBehaviour, IInteractable
{
    [SerializeField] private HUDController hudController;

    public bool CanInteract(uint playerId)
    {
        return GameManager.Instance != null && GameManager.Instance.IsPlaying;
    }

    public string GetInteractionPrompt()
    {
        return "Sleep";
    }

    public void Interact(uint playerId)
    {
        if (TimeManager.Instance == null) return;

        Debug.Log("[Bed] Zzz... Goodnight!");
        TimeManager.Instance.Sleep();

        // Update HUD clock after sleep
        if (hudController != null)
            hudController.UpdateClock(TimeManager.Instance.CurrentDay, TimeManager.Instance.CurrentSeason);

        Debug.Log($"[Bed] Good morning! {CalendarData.GetDateString(TimeManager.Instance.CurrentDay, TimeManager.Instance.CurrentSeason, TimeManager.Instance.CurrentYear)}");
    }
}
