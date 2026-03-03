using System.Collections.Generic;
using UnityEngine;

public class PlacedItem : MonoBehaviour, IInteractable, IBuildable
{
    [SerializeField] private ItemDefinition _sourceItem;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Vector3Int _gridPosition;

    private static readonly Dictionary<Vector3Int, PlacedItem> _occupiedPositions = new();

    public Vector2Int GridSize => Vector2Int.one;

    /// <summary>Sets source item and optionally wires the sprite renderer when created at runtime.</summary>
    public void Initialize(ItemDefinition item, SpriteRenderer sr = null)
    {
        _sourceItem = item;
        if (sr != null) _spriteRenderer = sr;
    }

    // IBuildable
    public bool CanPlaceAt(Vector3Int gridPosition, TileManager tileManager)
    {
        return tileManager.IsWalkable(gridPosition) && !IsOccupied(gridPosition);
    }

    public void OnPlaced(Vector3Int gridPosition)
    {
        _gridPosition = gridPosition;
        _occupiedPositions[gridPosition] = this;

        if (_spriteRenderer != null && _sourceItem != null)
            _spriteRenderer.sprite = _sourceItem.icon;

        transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0f);
    }

    public void OnRemoved()
    {
        _occupiedPositions.Remove(_gridPosition);
        EventBus.Publish(new ItemRemovedFromWorldEvent { Item = _sourceItem, GridPosition = _gridPosition });
        Destroy(gameObject);
    }

    // IInteractable
    public void Interact(uint playerId)
    {
        if (_sourceItem == null) return;
        InventoryManager.Instance.AddItem(_sourceItem, 1);
        OnRemoved();
    }

    public string GetInteractionPrompt()
    {
        return $"Pick up {_sourceItem?.itemName}";
    }

    public bool CanInteract(uint playerId) => true;

    /// <summary>Returns true if the given grid position is already occupied by a PlacedItem.</summary>
    public static bool IsOccupied(Vector3Int pos) => _occupiedPositions.ContainsKey(pos);
}
