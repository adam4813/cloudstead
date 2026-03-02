using UnityEngine;

/// <summary>
/// Interactable shop counter. Shows a merchant greeting, then opens the shop.
/// </summary>
public class ShopCounter : MonoBehaviour, IInteractable
{
    [SerializeField] private NPCDefinition merchant;

    // Multiplayer readiness
    [System.NonSerialized] public uint ownerId;

    public bool CanInteract(uint playerId)
    {
        return merchant != null && merchant.isMerchant;
    }

    public string GetInteractionPrompt()
    {
        return merchant != null ? $"Press E to shop" : "Browse Shop";
    }

    public void Interact(uint playerId)
    {
        if (merchant == null || !merchant.isMerchant) return;

        // Freeze player during greeting
        GameManager.Instance?.SetState(GameState.Dialogue);

        if (merchant.greetings != null && merchant.greetings.Length > 0)
        {
            string greeting = merchant.greetings[Random.Range(0, merchant.greetings.Length)];
            EventBus.Publish(new ShopGreetingEvent { Merchant = merchant, Greeting = greeting });
        }
        else
        {
            ShopManager.Instance?.OpenShop(merchant);
        }
    }
}
