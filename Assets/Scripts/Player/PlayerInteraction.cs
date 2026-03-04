using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask interactableMask;

    [SerializeField] private float pickupHoldDuration = 2f;

    private PlayerController playerController;
    private StaminaController staminaController;
    private IInteractable currentTarget;
    private uint ownerId = 0;

    private bool _isHoldingPickup;
    private float _holdTimer;

    public float HoldProgress => _isHoldingPickup ? Mathf.Clamp01(_holdTimer / pickupHoldDuration) : 0f;

    [SerializeField] private QuickbarUI quickbarUI;

    private TileCursor _tileCursor;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        staminaController = GetComponent<StaminaController>();
    }

    private void Start()
    {
        _tileCursor = FindFirstObjectByType<TileCursor>();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;

        FindNearestInteractable();
        HandlePickupHold();
    }

    /// <summary>Hold left-click on a PlacedItem to pick it up.</summary>
    private void HandlePickupHold()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed && currentTarget is PlacedItem placedTarget && placedTarget.CanInteract(ownerId))
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= pickupHoldDuration)
            {
                _holdTimer = 0f;
                _isHoldingPickup = false;
                placedTarget.Interact(ownerId);
            }
            else
            {
                _isHoldingPickup = true;
            }
        }
        else
        {
            _isHoldingPickup = false;
            _holdTimer = 0f;
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (!context.performed) return;

        // PlacedItems are picked up via hold-left-click, not E
        if (currentTarget != null && currentTarget is not PlacedItem && currentTarget.CanInteract(ownerId))
        {
            currentTarget.Interact(ownerId);
            EventBus.Publish(new InteractionEvent
            {
                Target = (currentTarget as MonoBehaviour)?.gameObject
            });
        }
        else if (currentTarget == null || currentTarget is PlacedItem)
        {
            TryUseActiveItem();
        }
    }

    private void TryUseActiveItem()
    {
        if (quickbarUI == null) return;

        var slot = quickbarUI.GetActiveSlotData();
        if (slot == null || slot.IsEmpty()) return;
        if (slot.item.staminaRestore <= 0) return;
        if (slot.item.category == ItemCategory.Seed) return;

        InventoryManager.Instance.RemoveItem(slot.item, 1);
        staminaController?.RestoreStamina(slot.item.staminaRestore);
        Debug.Log($"[Eat] Ate {slot.item.itemName}, restored {slot.item.staminaRestore} stamina");
    }

    private void FindNearestInteractable()
    {
        if (_tileCursor == null) return;

        Vector2 tileCenter = new Vector2(
            _tileCursor.HighlightedTile.x + 0.5f,
            _tileCursor.HighlightedTile.y + 0.5f);

        var hits = Physics2D.OverlapCircleAll(tileCenter, 0.4f, interactableMask);

        IInteractable nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract(ownerId))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = interactable;
                }
            }
        }

        currentTarget = nearest;
    }
}
