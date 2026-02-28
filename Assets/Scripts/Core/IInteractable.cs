public interface IInteractable
{
    void Interact(uint playerId);
    string GetInteractionPrompt();
    bool CanInteract(uint playerId);
}
