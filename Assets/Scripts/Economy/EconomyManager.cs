using UnityEngine;

public class EconomyManager : Singleton<EconomyManager>
{
    [SerializeField] private int startingGold = 500;

    private int playerGold;

    public int PlayerGold => playerGold;

    public override void Initialize()
    {
        playerGold = startingGold;
        PublishGoldChanged();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        playerGold += amount;
        PublishGoldChanged();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || playerGold < amount) return false;
        playerGold -= amount;
        PublishGoldChanged();
        return true;
    }

    public int GetGold() => playerGold;

    private void PublishGoldChanged()
    {
        EventBus.Publish(new GoldChangedEvent { NewAmount = playerGold });
    }
}
