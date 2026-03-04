using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image highlightBorder;

    private int slotIndex;
    private GameObject ghostIcon;
    private bool isDragging;

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (iconImage == null || !iconImage.enabled || iconImage.sprite == null)
            return;

        isDragging = true;

        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        ghostIcon = new GameObject("DragGhost");
        ghostIcon.transform.SetParent(rootCanvas.transform, false);
        ghostIcon.transform.SetAsLastSibling();

        var image = ghostIcon.AddComponent<Image>();
        image.sprite = iconImage.sprite;
        image.color = new Color(1f, 1f, 1f, 0.8f);
        image.raycastTarget = false;

        var rectTransform = ghostIcon.GetComponent<RectTransform>();
        rectTransform.sizeDelta = ((RectTransform)iconImage.transform).sizeDelta;
        rectTransform.position = eventData.position;

        iconImage.color = new Color(1f, 1f, 1f, 0.3f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || ghostIcon == null)
            return;

        ghostIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        if (ghostIcon != null)
            Destroy(ghostIcon);

        if (iconImage != null && iconImage.enabled)
            iconImage.color = Color.white;

        isDragging = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        var sourceSlot = eventData.pointerDrag.GetComponent<SlotUI>();
        if (sourceSlot == null || sourceSlot == this || !sourceSlot.isDragging)
            return;

        InventoryManager.Instance.SwapSlots(sourceSlot.SlotIndex, SlotIndex);
    }
}
