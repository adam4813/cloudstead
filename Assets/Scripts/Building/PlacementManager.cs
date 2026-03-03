using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : Singleton<PlacementManager>
{
    [SerializeField] private Material _ghostMaterial;

    public bool IsPlacing { get; private set; }

    private ItemDefinition _currentItem;
    private PlacementGhost _activeGhost;

    /// <summary>
    /// Enters placement mode for the given item, showing a ghost preview.
    /// </summary>
    public void EnterPlacementMode(ItemDefinition item)
    {
        if (item == null || !item.isPlaceable) return;

        ExitPlacementMode();

        IsPlacing = true;
        _currentItem = item;

        GameObject ghostGO = new GameObject("PlacementGhost");
        SpriteRenderer sr = ghostGO.AddComponent<SpriteRenderer>();
        sr.sprite = item.icon;
        sr.sortingLayerName = InteriorManager.Instance != null && InteriorManager.Instance.IsInsideInterior
            ? InteriorManager.ToInteriorLayer("Objects")
            : "Objects";
        sr.sortingOrder = 10;
        if (_ghostMaterial != null) sr.material = _ghostMaterial;

        _activeGhost = ghostGO.AddComponent<PlacementGhost>();
        _activeGhost.UpdateValidity(true);
    }

    /// <summary>
    /// Finalizes placement at the given grid position if the ghost is in a valid state.
    /// </summary>
    public void ConfirmPlacement(Vector3Int gridPos)
    {
        if (_activeGhost == null || !_activeGhost.IsValid)
        {
            Debug.Log("[PlacementManager] Cannot place here — position is invalid.");
            return;
        }

        InventoryManager.Instance.RemoveItem(_currentItem, 1);

        PlacedItem placedItem = CreatePlacedItemGO(_currentItem);
        placedItem.OnPlaced(gridPos);

        // Parent to interior objects container when placing inside a building
        if (InteriorManager.Instance != null && InteriorManager.Instance.IsInsideInterior)
        {
            var container = InteriorManager.Instance.CurrentInterior.ObjectsContainer;
            if (container != null)
                placedItem.transform.SetParent(container, true);
        }

        EventBus.Publish(new ItemPlacedEvent { Item = _currentItem, GridPosition = gridPos });

        if (InventoryManager.Instance.GetItemCount(_currentItem) == 0)
            ExitPlacementMode();
    }

    /// <summary>
    /// Exits placement mode and destroys the ghost preview.
    /// </summary>
    public void ExitPlacementMode()
    {
        if (_activeGhost != null)
        {
            Destroy(_activeGhost.gameObject);
            _activeGhost = null;
        }
        IsPlacing = false;
        _currentItem = null;
    }

    /// <summary>
    /// Reads the active quickbar slot and begins placement if the item is placeable.
    /// </summary>
    public void TryBeginFromQuickbar()
    {
        QuickbarUI quickbar = Object.FindFirstObjectByType<QuickbarUI>();
        if (quickbar == null) return;

        InventorySlot slot = quickbar.GetActiveSlotData();
        if (slot == null || slot.IsEmpty() || slot.item == null) return;
        if (!slot.item.isPlaceable) return;

        EnterPlacementMode(slot.item);
    }

    private void Update()
    {
        if (!IsPlacing || Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_activeGhost != null && Camera.main != null)
            {
                Vector2 screenPos = Mouse.current.position.ReadValue();
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                    new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
                worldPos.z = 0f;
                Vector3Int gridPos = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
                ConfirmPlacement(gridPos);
            }
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame ||
                 (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            ExitPlacementMode();
        }
    }

    private PlacedItem CreatePlacedItemGO(ItemDefinition item)
    {
        string sortingLayer = InteriorManager.Instance != null && InteriorManager.Instance.IsInsideInterior
            ? InteriorManager.ToInteriorLayer("Objects")
            : "Objects";

        if (item.placeablePrefab != null)
        {
            GameObject prefabGO = Instantiate(item.placeablePrefab);
            var sr2 = prefabGO.GetComponent<SpriteRenderer>();
            if (sr2 != null) sr2.sortingLayerName = sortingLayer;
            PlacedItem existing = prefabGO.GetComponent<PlacedItem>();
            if (existing != null)
            {
                existing.Initialize(item);
                return existing;
            }
            PlacedItem added = prefabGO.AddComponent<PlacedItem>();
            added.Initialize(item, sr2);
            return added;
        }

        GameObject go = new GameObject($"PlacedItem_{item.itemName}");
        go.layer = LayerMask.NameToLayer("Interactable");

        SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = sortingLayer;
        spriteRenderer.sortingOrder = 1;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);

        PlacedItem placedItem = go.AddComponent<PlacedItem>();
        placedItem.Initialize(item, spriteRenderer);

        return placedItem;
    }
}
