using UnityEngine;

/// <summary>
/// World-placed mailbox interactable on the player's farm.
/// Shows an unread indicator and opens MailboxUI on interact.
/// </summary>
public class Mailbox : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject unreadIndicator;
    [SerializeField] private MailboxUI mailboxUI;

    private void Start()
    {
        EventBus.Subscribe<MailReceivedEvent>(OnMailReceived);
        EventBus.Subscribe<MailReadEvent>(OnMailRead);
        UpdateIndicator();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MailReceivedEvent>(OnMailReceived);
        EventBus.Unsubscribe<MailReadEvent>(OnMailRead);
    }

    private void OnMailReceived(MailReceivedEvent evt) => UpdateIndicator();
    private void OnMailRead(MailReadEvent evt) => UpdateIndicator();

    private void UpdateIndicator()
    {
        if (unreadIndicator == null) return;
        int unread = MailboxManager.Instance != null ? MailboxManager.Instance.GetUnreadCount() : 0;
        unreadIndicator.SetActive(unread > 0);
    }

    public void Interact(uint playerId)
    {
        if (mailboxUI != null)
            mailboxUI.Open();
    }

    public string GetInteractionPrompt()
    {
        int unread = MailboxManager.Instance != null ? MailboxManager.Instance.GetUnreadCount() : 0;
        return unread > 0 ? $"Check Mail ({unread} new)" : "Check Mail";
    }

    public bool CanInteract(uint playerId) => true;
}
