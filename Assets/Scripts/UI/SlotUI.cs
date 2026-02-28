using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image highlightBorder;

    private int slotIndex;

    public int SlotIndex => slotIndex;

    public void Setup(int index)
    {
        slotIndex = index;
        Clear();
    }

    public void SetSlot(InventorySlot data)
    {
        if (data == null || data.IsEmpty())
        {
            Clear();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = data.item.icon;
            iconImage.enabled = data.item.icon != null;
            iconImage.color = Color.white;
        }

        if (countText != null)
        {
            countText.text = data.count > 1 ? data.count.ToString() : "";
            countText.enabled = data.count > 1;
        }
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        if (countText != null)
        {
            countText.text = "";
            countText.enabled = false;
        }
        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (highlightBorder != null)
            highlightBorder.enabled = active;
    }
}
