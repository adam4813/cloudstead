using System.Collections.Generic;
using UnityEngine;

public class MailboxManager : Singleton<MailboxManager>, ISaveable
{
    [SerializeField] private LetterDefinition[] scheduledLetters;

    private readonly List<LetterData> _letters = new();
    private readonly HashSet<string> _deliveredDefinitions = new();

    public IReadOnlyList<LetterData> Letters => _letters;

    public override void Initialize()
    {
        SaveManager.Instance?.Register(this);
        EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SaveManager.Instance?.Unregister(this);
        EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
    }

    private void OnDayStarted(DayStartedEvent evt)
    {
        if (scheduledLetters == null) return;

        foreach (var def in scheduledLetters)
        {
            if (def == null) continue;
            string defId = def.name;
            if (_deliveredDefinitions.Contains(defId)) continue;

            bool dayMatch = def.deliveryDay <= evt.Day;
            bool seasonMatch = (int)def.deliverySeason < 0 || def.deliverySeason == evt.Season;

            if (dayMatch && seasonMatch)
            {
                var letter = new LetterData
                {
                    id = defId,
                    sender = def.sender,
                    subject = def.subject,
                    body = def.body,
                    dayReceived = evt.Day,
                    seasonReceived = (int)evt.Season,
                    isRead = false,
                    attachedItemId = def.attachedItem != null ? def.attachedItem.ItemId : "",
                    attachedItemCount = def.attachedItemCount
                };

                DeliverLetter(letter);
                _deliveredDefinitions.Add(defId);
            }
        }
    }

    public void DeliverLetter(LetterData letter)
    {
        _letters.Add(letter);
        EventBus.Publish(new MailReceivedEvent { LetterId = letter.id, Sender = letter.sender });
    }

    public int GetUnreadCount()
    {
        int count = 0;
        foreach (var l in _letters)
            if (!l.isRead) count++;
        return count;
    }

    public void MarkAsRead(string letterId)
    {
        for (int i = 0; i < _letters.Count; i++)
        {
            if (_letters[i].id == letterId)
            {
                _letters[i].isRead = true;
                EventBus.Publish(new MailReadEvent { LetterId = letterId });
                return;
            }
        }
    }

    public bool ClaimAttachment(string letterId)
    {
        for (int i = 0; i < _letters.Count; i++)
        {
            var letter = _letters[i];
            if (letter.id != letterId) continue;
            if (string.IsNullOrEmpty(letter.attachedItemId)) return false;

            var db = GameBootstrapper.Database;
            if (db == null) return false;

            var item = db.GetItem(letter.attachedItemId);
            if (item == null) return false;

            InventoryManager.Instance?.AddItem(item, letter.attachedItemCount);
            letter.attachedItemId = "";
            letter.attachedItemCount = 0;
            _letters[i] = letter;
            return true;
        }
        return false;
    }

    #region ISaveable

    public string SaveState()
    {
        var data = new MailboxSaveData
        {
            letters = _letters.ToArray(),
            deliveredDefs = new string[_deliveredDefinitions.Count]
        };
        int idx = 0;
        foreach (var d in _deliveredDefinitions)
            data.deliveredDefs[idx++] = d;

        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<MailboxSaveData>(json);
        if (data == null) return;

        _letters.Clear();
        if (data.letters != null)
            _letters.AddRange(data.letters);

        _deliveredDefinitions.Clear();
        if (data.deliveredDefs != null)
            foreach (var d in data.deliveredDefs)
                _deliveredDefinitions.Add(d);
    }

    [System.Serializable]
    private class MailboxSaveData
    {
        public LetterData[] letters;
        public string[] deliveredDefs;
    }

    #endregion
}
