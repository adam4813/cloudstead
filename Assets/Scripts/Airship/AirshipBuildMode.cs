using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

/// <summary>
/// Allows the player to place ship parts (tiles) and ship items on a docked airship.
/// Toggle with B while aboard a docked airship. Left-click places, right-click removes.
/// Implements ISaveable to persist tile modifications across sessions.
/// </summary>
public class AirshipBuildMode : MonoBehaviour, ISaveable
{
    [Header("Tilemaps")]
    [SerializeField] [Required] private Tilemap floorTilemap;
    [SerializeField] private Tilemap decorationTilemap;
    [SerializeField] private Tilemap wallsTilemap;

    [Header("Ghost")]
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    private AirshipController airship;
    private bool isActive;
    private GameObject ghostGO;
    private SpriteRenderer ghostSR;
    private ItemDefinition currentItem;
    private bool ghostValid;

    private readonly List<AirshipTileSave> placedTiles = new();

    public bool IsActive => isActive;

    private void Awake()
    {
        airship = GetComponent<AirshipController>();
    }

    private void Start()
    {
        SaveManager.Instance?.Register(this);
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // Toggle build mode with B
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (isActive)
                ExitBuildMode();
            else
                TryEnterBuildMode();
            return;
        }

        if (!isActive) return;

        UpdateCurrentItem();
        UpdateGhost();
        HandleInput();
    }

    private void TryEnterBuildMode()
    {
        if (airship == null) return;
        if (airship.IsFlying) return;

        // Player must be associated with this airship
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null || player.CurrentAirship != airship) return;

        isActive = true;
        GameManager.Instance.SetState(GameState.Building);
        EventBus.Publish(new AirshipBuildModeEnteredEvent { AirshipId = airship.AirshipId });
    }

    private void ExitBuildMode()
    {
        isActive = false;
        DestroyGhost();
        currentItem = null;
        GameManager.Instance.SetState(GameState.Playing);
        EventBus.Publish(new AirshipBuildModeExitedEvent { AirshipId = airship.AirshipId });
    }

    private void UpdateCurrentItem()
    {
        var quickbar = Object.FindFirstObjectByType<QuickbarUI>();
        if (quickbar == null) { SetCurrentItem(null); return; }

        InventorySlot slot = quickbar.GetActiveSlotData();
        if (slot == null || slot.IsEmpty() || slot.item == null)
        {
            SetCurrentItem(null);
            return;
        }

        var item = slot.item;
        bool isAirshipItem = item.airshipTile != null
            || (item.isPlaceable && item.requiresBuildMode)
            || item.isPlaceable;

        SetCurrentItem(isAirshipItem ? item : null);
    }

    private void SetCurrentItem(ItemDefinition item)
    {
        if (currentItem == item) return;
        currentItem = item;

        if (currentItem == null)
        {
            DestroyGhost();
            return;
        }

        CreateGhost();
    }

    private void CreateGhost()
    {
        DestroyGhost();
        if (currentItem == null) return;

        ghostGO = new GameObject("AirshipBuildGhost");
        ghostSR = ghostGO.AddComponent<SpriteRenderer>();
        ghostSR.sprite = currentItem.icon;
        ghostSR.sortingLayerName = "Objects";
        ghostSR.sortingOrder = 100;
        ghostSR.color = validColor;
    }

    private void DestroyGhost()
    {
        if (ghostGO != null)
        {
            Destroy(ghostGO);
            ghostGO = null;
            ghostSR = null;
        }
        ghostValid = false;
    }

    private void UpdateGhost()
    {
        if (ghostGO == null || Mouse.current == null || Camera.main == null) return;

        Vector3 worldPos = GetMouseWorldPos();
        Tilemap targetMap = GetTargetTilemap(currentItem);
        if (targetMap == null) { ghostValid = false; return; }

        Vector3Int cellPos = targetMap.WorldToCell(worldPos);
        Vector3 snapped = targetMap.GetCellCenterWorld(cellPos);
        ghostGO.transform.position = snapped;

        ghostValid = IsPlacementValid(currentItem, targetMap, cellPos);
        ghostSR.color = ghostValid ? validColor : invalidColor;
    }

    private void HandleInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && currentItem != null)
        {
            TryPlace();
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryRemove();
        }
    }

    private void TryPlace()
    {
        if (!ghostValid || currentItem == null) return;

        Vector3 worldPos = GetMouseWorldPos();

        if (currentItem.airshipTile != null)
        {
            PlaceTile(worldPos);
        }
        else if (currentItem.isPlaceable)
        {
            PlaceItem(worldPos);
        }
    }

    private void PlaceTile(Vector3 worldPos)
    {
        Tilemap map = GetTargetTilemap(currentItem);
        if (map == null) return;

        Vector3Int cellPos = map.WorldToCell(worldPos);

        // Don't overwrite existing tiles on the same layer
        if (map.GetTile(cellPos) != null) return;

        if (!IsPlacementValid(currentItem, map, cellPos)) return;

        InventoryManager.Instance.RemoveItem(currentItem, 1);
        map.SetTile(cellPos, currentItem.airshipTile);

        placedTiles.Add(new AirshipTileSave
        {
            x = cellPos.x,
            y = cellPos.y,
            itemId = currentItem.ItemId,
            layer = currentItem.airshipLayer
        });

        SyncColliders();

        EventBus.Publish(new AirshipTilePlacedEvent
        {
            AirshipId = airship.AirshipId,
            GridPosition = cellPos,
            Layer = currentItem.airshipLayer
        });

        // If no more of this item, clear selection
        if (InventoryManager.Instance.GetItemCount(currentItem) == 0)
        {
            currentItem = null;
            DestroyGhost();
        }
    }

    private void PlaceItem(Vector3 worldPos)
    {
        Tilemap map = floorTilemap;
        if (map == null) return;

        Vector3Int cellPos = map.WorldToCell(worldPos);

        // Items must be placed on existing floor tiles
        if (map.GetTile(cellPos) == null) return;
        if (PlacedItem.IsOccupied(cellPos)) return;

        InventoryManager.Instance.RemoveItem(currentItem, 1);

        // Create placed item directly, parented to the airship
        GameObject go = new GameObject($"PlacedItem_{currentItem.itemName}");
        go.layer = LayerMask.NameToLayer("Interactable");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = currentItem.icon;
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 1;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);

        PlacedItem placed = go.AddComponent<PlacedItem>();
        placed.Initialize(currentItem, sr);
        placed.OnPlaced(cellPos);
        go.transform.SetParent(transform, true);

        EventBus.Publish(new ItemPlacedEvent { Item = currentItem, GridPosition = cellPos });

        if (InventoryManager.Instance.GetItemCount(currentItem) == 0)
        {
            currentItem = null;
            DestroyGhost();
        }
    }

    private void TryRemove()
    {
        Vector3 worldPos = GetMouseWorldPos();

        // First check for placed items at cursor
        if (TryPickUpItem(worldPos)) return;

        // Then check for removable tiles
        TryRemoveTile(worldPos);
    }

    private bool TryPickUpItem(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Interactable"));
        if (hit == null) return false;

        PlacedItem placed = hit.GetComponent<PlacedItem>();
        if (placed == null || placed.SourceItem == null) return false;

        // Only pick up items parented to this airship
        if (placed.transform.parent != transform) return false;

        InventoryManager.Instance.AddItem(placed.SourceItem, 1);
        placed.OnRemoved(); // handles event, occupancy cleanup, and Destroy
        return true;
    }

    private void TryRemoveTile(Vector3 worldPos)
    {
        // Check each layer from top to bottom: Walls → Decoration → Floor
        if (TryRemoveTileFromMap(wallsTilemap, AirshipTilemapLayer.Walls, worldPos)) return;
        if (TryRemoveTileFromMap(decorationTilemap, AirshipTilemapLayer.Decoration, worldPos)) return;
        TryRemoveTileFromMap(floorTilemap, AirshipTilemapLayer.Floor, worldPos);
    }

    private bool TryRemoveTileFromMap(Tilemap map, AirshipTilemapLayer layer, Vector3 worldPos)
    {
        if (map == null) return false;

        Vector3Int cellPos = map.WorldToCell(worldPos);
        TileBase tile = map.GetTile(cellPos);
        if (tile == null) return false;

        // Find the matching placed-tile entry
        int idx = placedTiles.FindIndex(t => t.x == cellPos.x && t.y == cellPos.y && t.layer == layer);
        if (idx < 0) return false; // only remove player-placed tiles

        string itemId = placedTiles[idx].itemId;
        placedTiles.RemoveAt(idx);

        map.SetTile(cellPos, null);
        SyncColliders();

        // Return item to inventory
        var db = GameBootstrapper.Database;
        if (db != null)
        {
            var itemDef = db.GetItem(itemId);
            if (itemDef != null)
                InventoryManager.Instance.AddItem(itemDef, 1);
        }

        return true;
    }

    private bool IsPlacementValid(ItemDefinition item, Tilemap targetMap, Vector3Int cellPos)
    {
        if (item.airshipTile != null)
        {
            // Ship parts: must be adjacent to an existing floor tile (or on the floor tilemap itself)
            if (targetMap.GetTile(cellPos) != null) return false; // occupied

            return HasAdjacentFloorTile(cellPos);
        }
        else
        {
            // Placed items: must be on an existing floor tile and not occupied
            if (floorTilemap == null) return false;
            if (floorTilemap.GetTile(cellPos) == null) return false;
            if (PlacedItem.IsOccupied(cellPos)) return false;
            return true;
        }
    }

    private bool HasAdjacentFloorTile(Vector3Int cellPos)
    {
        if (floorTilemap == null) return false;

        // Allow placement on top of an existing floor tile (for decoration/wall layers)
        if (floorTilemap.GetTile(cellPos) != null) return true;

        // Check 4-directional neighbors on the floor tilemap
        Vector3Int[] offsets = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        foreach (var offset in offsets)
        {
            if (floorTilemap.GetTile(cellPos + offset) != null) return true;
        }
        return false;
    }

    private Tilemap GetTargetTilemap(ItemDefinition item)
    {
        if (item == null) return null;

        if (item.airshipTile != null)
        {
            return item.airshipLayer switch
            {
                AirshipTilemapLayer.Floor => floorTilemap,
                AirshipTilemapLayer.Decoration => decorationTilemap,
                AirshipTilemapLayer.Walls => wallsTilemap,
                _ => floorTilemap
            };
        }

        // For placeable items, use the floor tilemap for grid snapping
        return floorTilemap;
    }

    private void SyncColliders()
    {
        Physics2D.SyncTransforms();
        var composite = GetComponentInChildren<CompositeCollider2D>();
        if (composite != null)
            composite.GenerateGeometry();
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
        worldPos.z = 0f;
        return worldPos;
    }

    #region ISaveable

    public string SaveKey => $"AirshipBuildMode:{airship?.AirshipId ?? "unknown"}";

    public string SaveState()
    {
        return JsonUtility.ToJson(new AirshipBuildSaveData
        {
            tiles = placedTiles.ToArray()
        });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<AirshipBuildSaveData>(json);
        if (data?.tiles == null) return;

        placedTiles.Clear();
        var db = GameBootstrapper.Database;
        if (db == null)
        {
            Debug.LogWarning("[AirshipBuildMode] RestoreState: GameDatabase not available");
            return;
        }

        foreach (var entry in data.tiles)
        {
            var itemDef = db.GetItem(entry.itemId);
            if (itemDef == null || itemDef.airshipTile == null)
            {
                Debug.LogWarning($"[AirshipBuildMode] RestoreState: item '{entry.itemId}' not found or has no airshipTile");
                continue;
            }

            Tilemap map = entry.layer switch
            {
                AirshipTilemapLayer.Floor => floorTilemap,
                AirshipTilemapLayer.Decoration => decorationTilemap,
                AirshipTilemapLayer.Walls => wallsTilemap,
                _ => floorTilemap
            };

            if (map == null) continue;

            Vector3Int cellPos = new Vector3Int(entry.x, entry.y, 0);
            map.SetTile(cellPos, itemDef.airshipTile);
            placedTiles.Add(entry);
        }

        SyncColliders();
        Debug.Log($"[AirshipBuildMode] RestoreState: restored {placedTiles.Count} tiles");
    }

    [System.Serializable]
    private class AirshipBuildSaveData
    {
        public AirshipTileSave[] tiles;
    }

    [System.Serializable]
    private class AirshipTileSave
    {
        public int x;
        public int y;
        public string itemId;
        public AirshipTilemapLayer layer;
    }

    #endregion
}
