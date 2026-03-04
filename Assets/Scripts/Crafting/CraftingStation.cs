using UnityEngine;

public class CraftingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Workbench";
    [SerializeField] private AudioClip openSound;

    private CraftingUI _craftingUI;

    private void Start()
    {
        _craftingUI = FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include);
    }

    public bool CanInteract(uint playerId) => true;

    public string GetInteractionPrompt() => $"Use {stationName}";

    public void Interact(uint playerId)
    {
        if (_craftingUI == null)
            _craftingUI = FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include);

        if (openSound != null)
            AudioSource.PlayClipAtPoint(openSound, transform.position);

        _craftingUI?.Open();
        GameManager.Instance?.SetState(GameState.Menu);
    }
}
