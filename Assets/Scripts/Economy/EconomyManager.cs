using UnityEngine;

public class EconomyManager : Singleton<EconomyManager>, ISaveable
{
    [SerializeField] private int startingGold = 500;

    private int playerGold;

    public int PlayerGold => playerGold;

    public override void Initialize()
    {
        playerGold = startingGold;
        PublishGoldChanged();

        if (SaveManager.Instance != null)
            SaveManager.Instance.Register(this);
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

    public string SaveState()
    {
        return JsonUtility.ToJson(new EconomySaveData { playerGold = playerGold });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<EconomySaveData>(json);
        if (data == null) return;

        playerGold = data.playerGold;
        PublishGoldChanged();
    }

    [System.Serializable]
    private class EconomySaveData
    {
        public int playerGold;
    }
}
