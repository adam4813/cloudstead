using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : Singleton<PlacementManager>, ISaveable
{
    [SerializeField] private Material _ghostMaterial;

    public bool IsPlacing { get; private set; }

    private ItemDefinition _currentItem;
    private PlacementGhost _activeGhost;
    private readonly List<PlacedItem> _trackedItems = new();

    private IPlacementContext _context;
    private CloudPlacementContext _defaultContext;

    /// <summary>The active placement context. Falls back to CloudPlacementContext.</summary>
    public IPlacementContext Context => _context ?? _defaultContext;

    /// <summary>Sets the default cloud placement parent. Called by CloudIsland on Start.</summary>
    public void SetDefaultParent(Transform parent)
    {
        _defaultContext = new CloudPlacementContext(parent);
    }

    /// <summary>
    /// Scans all children of the given container for PlacedItem components,
    /// registers them in the occupied-positions map, and tracks them for save/restore.
    /// Call from CloudIsland/BuildingInterior Start() for scene-placed objects.
    /// Skipped when loading a save — RestoreState handles reconstruction.
    /// </summary>
    public void RegisterExistingItems(Transform container)
    {
        if (container == null) return; 

        string sortingLayer = Context.SortingLayer;

        foreach (Transform child in container)
        {
            var placed = child.GetComponent<PlacedItem>();
            if (placed == null) continue;

            // Ensure the object is detectable by physics queries
            child.gameObject.layer = LayerMask.NameToLayer("Interactable");
            if (child.GetComponent<Collider2D>() == null)
            {
                var col = child.gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.8f, 0.8f);
            }

            // Set sorting layer to match the active placement context
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingLayerName = sortingLayer;

            Vector3Int gridPos = new Vector3Int(
                Mathf.FloorToInt(child.position.x),
                Mathf.FloorToInt(child.position.y), 0);

            placed.OnPlaced(gridPos);
            if (!_trackedItems.Contains(placed))
                _trackedItems.Add(placed);
        }
    }

    public override void Initialize()
    {
        _defaultContext ??= new CloudPlacementContext();
        SaveManager.Instance?.Register(this);
        EventBus.Subscribe<ItemRemovedFromWorldEvent>(OnItemRemoved);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SaveManager.Instance?.Unregister(this);
        EventBus.Unsubscribe<ItemRemovedFromWorldEvent>(OnItemRemoved);
    }

    private void OnItemRemoved(ItemRemovedFromWorldEvent evt)
    {
        _trackedItems.RemoveAll(p => p == null);
    }

    /// <summary>Sets the active placement context. Pass null to revert to the default cloud context.</summary>
    public void SetContext(IPlacementContext context)
    {
        if (IsPlacing)
            ExitPlacementMode();
        _context = context;
    }

    /// <summary>
    /// Enters placement mode for the given item, showing a ghost preview.
    /// </summary>
    public void EnterPlacementMode(ItemDefinition item)
    {
        if (item == null || !item.isPlaceable) return;

        // Validate placement zone
        PlacementZone currentZone = GetCurrentZone();
        if ((item.allowedZones & currentZone) == 0)
        {
            Debug.Log($"[PlacementManager] {item.itemName} cannot be placed in {currentZone}.");
            return;
        }

        ExitPlacementMode();

        IsPlacing = true;
        _currentItem = item;

        GameObject ghostGO = new GameObject("PlacementGhost");
        SpriteRenderer sr = ghostGO.AddComponent<SpriteRenderer>();
        sr.sprite = item.icon;
        sr.sortingLayerName = Context.SortingLayer;
        sr.sortingOrder = 10;
        if (_ghostMaterial != null) sr.material = _ghostMaterial;

        _activeGhost = ghostGO.AddComponent<PlacementGhost>();
        _activeGhost.SetContext(Context);
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

        PlaceItemAt(_currentItem, gridPos);

        if (InventoryManager.Instance.GetItemCount(_currentItem) == 0)
            ExitPlacementMode();
    }

    /// <summary>
    /// Creates a placed item at the given grid position using the active context for
    /// sorting, parenting, and positioning. Removes the item from inventory, tracks
    /// the result for save/restore. Can be called directly by external systems
    /// (e.g., AirshipBuildMode) that handle their own ghost/input.
    /// </summary>
    public PlacedItem PlaceItemAt(ItemDefinition item, Vector3Int gridPos)
    {
        InventoryManager.Instance.RemoveItem(item, 1);
        return SpawnPlacedItem(item, gridPos);
    }

    /// <summary>
    /// Places an item at the given grid position without removing it from inventory.
    /// Used for initial world setup and scripted placements.
    /// </summary>
    public PlacedItem SpawnItemAt(ItemDefinition item, Vector3Int gridPos)
    {
        return SpawnPlacedItem(item, gridPos);
    }

    private PlacedItem SpawnPlacedItem(ItemDefinition item, Vector3Int gridPos)
    {
        PlacedItem placedItem = CreatePlacedItemGO(item, Context.SortingLayer);
        placedItem.OnPlaced(gridPos);

        Transform parent = Context.GetParent();
        if (parent != null)
            placedItem.transform.SetParent(parent, false);

        placedItem.transform.position = Context.CellToWorldPosition(gridPos);

        _trackedItems.Add(placedItem);

        EventBus.Publish(new ItemPlacedEvent { Item = item, GridPosition = gridPos });
        return placedItem;
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
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameState.Playing &&
            GameManager.Instance.CurrentState != GameState.Building) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_activeGhost != null && Camera.main != null)
            {
                Vector3 worldPos = GetMouseWorldPos();
                Vector3Int gridPos = Context.WorldToCell(worldPos);
                ConfirmPlacement(gridPos);
            }
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame ||
                 (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            ExitPlacementMode();
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
        worldPos.z = 0f;
        return worldPos;
    }

    /// <summary>Determines the current placement zone based on player context.</summary>
    public static PlacementZone GetCurrentZone()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            if (player.CurrentAirship != null)
                return PlacementZone.Airship;
            if (player.CurrentInterior != null)
                return PlacementZone.Interior;
        }
        return PlacementZone.Exterior;
    }

    #region ISaveable

    public string SaveState()
    {
        _trackedItems.RemoveAll(p => p == null);
        var entries = new PlacedItemEntry[_trackedItems.Count];

        for (int i = 0; i < _trackedItems.Count; i++)
        {
            var item = _trackedItems[i];
            entries[i] = new PlacedItemEntry
            {
                itemId = item.SourceItem != null ? item.SourceItem.ItemId : "",
                gridX = item.GridPosition.x,
                gridY = item.GridPosition.y,
                context = ResolveContext(item)
            };
        }

        return JsonUtility.ToJson(new PlacementSaveData { items = entries });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<PlacementSaveData>(json);
        if (data?.items == null) return;

        // Destroy existing player-placed items
        foreach (var item in _trackedItems)
            if (item != null) DestroyImmediate(item.gameObject);
        _trackedItems.Clear();
        PlacedItem.ClearOccupied();

        var db = GameBootstrapper.Database;
        if (db == null)
        {
            Debug.LogWarning("[PlacementManager] RestoreState: GameDatabase not available");
            return;
        }

        // Build lookup for interior containers by buildingId
        var interiors = new Dictionary<string, BuildingInterior>();
        foreach (var bi in FindObjectsByType<BuildingInterior>(FindObjectsSortMode.None))
        {
            if (!string.IsNullOrEmpty(bi.BuildingId))
                interiors[bi.BuildingId] = bi;
        }

        // Build lookup for airships by airshipId
        var airships = new Dictionary<string, AirshipController>();
        foreach (var ac in FindObjectsByType<AirshipController>(FindObjectsSortMode.None))
        {
            if (!string.IsNullOrEmpty(ac.AirshipId))
                airships[ac.AirshipId] = ac;
        }

        // Build lookup for clouds by cloudId
        var clouds = new Dictionary<string, CloudIsland>();
        foreach (var cg in FindObjectsByType<CloudIsland>(FindObjectsSortMode.None))
        {
            if (!string.IsNullOrEmpty(cg.CloudId))
                clouds[cg.CloudId] = cg;
        }

        foreach (var entry in data.items)
        {
            var itemDef = db.GetItem(entry.itemId);
            if (itemDef == null)
            {
                Debug.LogWarning($"[PlacementManager] RestoreState: item '{entry.itemId}' not found in GameDatabase");
                continue;
            }

            var gridPos = new Vector3Int(entry.gridX, entry.gridY, 0);

            // Determine sorting layer and parent from context
            string sortingLayer = "Objects";
            Transform parent = null;
            string ctx = entry.context ?? "cloud:home";

            if (ctx.StartsWith("interior:"))
            {
                string buildingId = ctx.Substring("interior:".Length);
                sortingLayer = InteriorManager.ToInteriorLayer("Objects");
                if (interiors.TryGetValue(buildingId, out var bi) && bi.ObjectsContainer != null)
                    parent = bi.ObjectsContainer;
            }
            else if (ctx.StartsWith("airship:"))
            {
                string id = ctx.Substring("airship:".Length);
                if (airships.TryGetValue(id, out var ac))
                    parent = ac.transform;
            }
            else if (ctx.StartsWith("cloud:"))
            {
                string id = ctx.Substring("cloud:".Length);
                if (clouds.TryGetValue(id, out var cg))
                    parent = cg.ObstacleParent != null ? cg.ObstacleParent : cg.transform;
            }

            PlacedItem placed = CreatePlacedItemGO(itemDef, sortingLayer);
            placed.OnPlaced(gridPos);

            if (parent != null)
            {
                if (ctx.StartsWith("airship:"))
                {
                    // Airship can be rotated/moved — use its tilemap for correct positioning
                    string id = ctx.Substring("airship:".Length);
                    var tilemap = airships.TryGetValue(id, out var ship)
                        ? ship.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>()
                        : null;

                    placed.transform.SetParent(parent, false);
                    placed.transform.position = tilemap != null
                        ? tilemap.GetCellCenterWorld(gridPos)
                        : parent.TransformPoint(new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f));
                }
                else
                {
                    placed.transform.SetParent(parent, true);
                }
            }

            _trackedItems.Add(placed);
        }

        Debug.Log($"[PlacementManager] RestoreState: restored {_trackedItems.Count} placed items");
    }

    /// <summary>Determines the context string for a placed item based on its parent hierarchy.</summary>
    private string ResolveContext(PlacedItem item)
    {
        if (item.transform.parent == null) return "cloud:home";

        // Check if parented under a BuildingInterior's objects container
        var bi = item.GetComponentInParent<BuildingInterior>();
        if (bi != null && !string.IsNullOrEmpty(bi.BuildingId))
            return $"interior:{bi.BuildingId}";

        // Check if parented under an airship
        var ac = item.GetComponentInParent<AirshipController>();
        if (ac != null)
            return $"airship:{ac.AirshipId}";

        // Check if parented under a cloud island
        var cloud = item.GetComponentInParent<CloudIsland>();
        if (cloud != null)
            return $"cloud:{cloud.CloudId}";

        return "cloud:home";
    }

    /// <summary>Creates a PlacedItem GO with explicit sorting layer (used during restore).</summary>
    private PlacedItem CreatePlacedItemGO(ItemDefinition item, string sortingLayer)
    {
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

    [System.Serializable]
    private class PlacementSaveData
    {
        public PlacedItemEntry[] items;
    }

    [System.Serializable]
    private class PlacedItemEntry
    {
        public string itemId;
        public int gridX;
        public int gridY;
        public string context; // "cloud:{cloudId}", "interior:{buildingId}", "airship:{airshipId}"
    }

    #endregion
}
