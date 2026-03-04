using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

/// <summary>
/// Tile-only editor for airship structural parts (hull, wings, railings, etc.).
/// Entered via dock interaction (AirshipDock), not inventory. Ship parts are
/// purchased with gold and placed directly onto the airship's tilemaps.
///
/// Item placement on the airship deck (helm, lantern, etc.) is handled by
/// PlacementManager with an AirshipPlacementContext — same UX as everywhere else.
///
/// Implements ISaveable to persist player-placed tiles across sessions.
/// </summary>
public class AirshipBuildMode : MonoBehaviour, ISaveable
{
    [Header("Tilemaps")]
    [SerializeField] [Required] private Tilemap floorTilemap;
    [SerializeField] private Tilemap decorationTilemap;
    [SerializeField] private Tilemap wallsTilemap;

    [Header("Ship Parts")]
    [Tooltip("Available structural parts the player can purchase and place.")]
    [SerializeField] private ShipPartDefinition[] availableParts;

    [Header("Ghost")]
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    private AirshipController airship;
    private bool isActive;
    private GameObject ghostGO;
    private SpriteRenderer ghostSR;
    private int selectedPartIndex = -1;
    private bool ghostValid;

    private readonly List<AirshipTileSave> placedTiles = new();

    public bool IsActive => isActive;
    public ShipPartDefinition[] AvailableParts => availableParts;
    public int SelectedPartIndex => selectedPartIndex;

    private ShipPartDefinition SelectedPart =>
        selectedPartIndex >= 0 && selectedPartIndex < availableParts.Length
            ? availableParts[selectedPartIndex]
            : null;

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
        if (!isActive) return;

        HandlePartSelection();
        UpdateTileGhost();
        HandleInput();
    }

    /// <summary>Called by AirshipDock when the player interacts with the dock while aboard.</summary>
    public void EnterEditMode()
    {
        if (airship == null || airship.IsFlying) return;

        isActive = true;
        selectedPartIndex = availableParts != null && availableParts.Length > 0 ? 0 : -1;

        if (SelectedPart != null)
            CreateTileGhost();

        PlacementManager.Instance?.ExitPlacementMode();

        GameManager.Instance.SetState(GameState.Building);
        EventBus.Publish(new AirshipBuildModeEnteredEvent { AirshipId = airship.AirshipId });
        Debug.Log($"[AirshipBuildMode] Entered edit mode. {availableParts?.Length ?? 0} parts available.");
    }

    public void ExitEditMode()
    {
        isActive = false;
        DestroyTileGhost();
        selectedPartIndex = -1;

        GameManager.Instance.SetState(GameState.Playing);
        EventBus.Publish(new AirshipBuildModeExitedEvent { AirshipId = airship.AirshipId });
    }

    #region Part Selection

    private void HandlePartSelection()
    {
        if (Keyboard.current == null || availableParts == null || availableParts.Length == 0) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitEditMode();
            return;
        }

        for (int i = 0; i < Mathf.Min(availableParts.Length, 9); i++)
        {
            if (Keyboard.current[Key.Digit1 + i].wasPressedThisFrame)
            {
                SelectPart(i);
                return;
            }
        }

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0f)
                SelectPart((selectedPartIndex + 1) % availableParts.Length);
            else if (scroll < 0f)
                SelectPart((selectedPartIndex - 1 + availableParts.Length) % availableParts.Length);
        }
    }

    private void SelectPart(int index)
    {
        if (index < 0 || index >= availableParts.Length) return;
        selectedPartIndex = index;
        CreateTileGhost();
    }

    #endregion

    #region Ghost Preview

    private void CreateTileGhost()
    {
        DestroyTileGhost();
        var part = SelectedPart;
        if (part == null) return;

        ghostGO = new GameObject("AirshipTileGhost");
        ghostSR = ghostGO.AddComponent<SpriteRenderer>();
        ghostSR.sprite = part.icon;
        ghostSR.sortingLayerName = "Objects";
        ghostSR.sortingOrder = 100;
        ghostSR.color = validColor;
    }

    private void DestroyTileGhost()
    {
        if (ghostGO != null)
        {
            Destroy(ghostGO);
            ghostGO = null;
            ghostSR = null;
        }
        ghostValid = false;
    }

    private void UpdateTileGhost()
    {
        if (ghostGO == null || Mouse.current == null || Camera.main == null) return;

        var part = SelectedPart;
        if (part == null) { ghostValid = false; return; }

        Vector3 worldPos = GetMouseWorldPos();
        Tilemap targetMap = GetTargetTilemap(part.layer);
        if (targetMap == null) { ghostValid = false; return; }

        Vector3Int cellPos = targetMap.WorldToCell(worldPos);
        ghostGO.transform.position = targetMap.GetCellCenterWorld(cellPos);

        ghostValid = IsTilePlacementValid(targetMap, cellPos);
        ghostSR.color = ghostValid ? validColor : invalidColor;
    }

    #endregion

    #region Input

    private void HandleInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPlaceTile();
        else if (Mouse.current.rightButton.wasPressedThisFrame)
            TryRemoveTile(GetMouseWorldPos());
    }

    #endregion

    #region Tile Placement & Removal

    private void TryPlaceTile()
    {
        var part = SelectedPart;
        if (!ghostValid || part == null) return;

        if (part.goldCost > 0 && EconomyManager.Instance.GetGold() < part.goldCost)
        {
            Debug.Log($"[AirshipBuildMode] Not enough gold. Need {part.goldCost}, have {EconomyManager.Instance.GetGold()}.");
            return;
        }

        Vector3 worldPos = GetMouseWorldPos();
        Tilemap map = GetTargetTilemap(part.layer);
        if (map == null) return;

        Vector3Int cellPos = map.WorldToCell(worldPos);
        if (map.GetTile(cellPos) != null) return;
        if (!IsTilePlacementValid(map, cellPos)) return;

        if (part.goldCost > 0)
            EconomyManager.Instance.SpendGold(part.goldCost);

        map.SetTile(cellPos, part.tile);
        placedTiles.Add(new AirshipTileSave
        {
            x = cellPos.x,
            y = cellPos.y,
            partName = part.partName,
            layer = part.layer
        });

        SyncColliders();
        EventBus.Publish(new AirshipTilePlacedEvent
        {
            AirshipId = airship.AirshipId,
            GridPosition = cellPos,
            Layer = part.layer
        });
    }

    private void TryRemoveTile(Vector3 worldPos)
    {
        if (TryRemoveTileFromMap(wallsTilemap, AirshipTilemapLayer.Walls, worldPos)) return;
        if (TryRemoveTileFromMap(decorationTilemap, AirshipTilemapLayer.Decoration, worldPos)) return;
        TryRemoveTileFromMap(floorTilemap, AirshipTilemapLayer.Floor, worldPos);
    }

    private bool TryRemoveTileFromMap(Tilemap map, AirshipTilemapLayer layer, Vector3 worldPos)
    {
        if (map == null) return false;

        Vector3Int cellPos = map.WorldToCell(worldPos);
        if (map.GetTile(cellPos) == null) return false;

        int idx = placedTiles.FindIndex(t => t.x == cellPos.x && t.y == cellPos.y && t.layer == layer);
        if (idx < 0) return false;

        var removed = placedTiles[idx];
        placedTiles.RemoveAt(idx);
        map.SetTile(cellPos, null);
        SyncColliders();

        // Refund gold
        var part = FindPartByName(removed.partName);
        if (part != null && part.goldCost > 0)
            EconomyManager.Instance.AddGold(part.goldCost);

        return true;
    }

    #endregion

    #region Validation

    private bool IsTilePlacementValid(Tilemap targetMap, Vector3Int cellPos)
    {
        if (targetMap.GetTile(cellPos) != null) return false;
        return HasAdjacentFloorTile(cellPos);
    }

    private bool HasAdjacentFloorTile(Vector3Int cellPos)
    {
        if (floorTilemap == null) return false;
        if (floorTilemap.GetTile(cellPos) != null) return true;

        Vector3Int[] offsets = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        foreach (var offset in offsets)
        {
            if (floorTilemap.GetTile(cellPos + offset) != null) return true;
        }
        return false;
    }

    private Tilemap GetTargetTilemap(AirshipTilemapLayer layer)
    {
        return layer switch
        {
            AirshipTilemapLayer.Floor => floorTilemap,
            AirshipTilemapLayer.Decoration => decorationTilemap,
            AirshipTilemapLayer.Walls => wallsTilemap,
            _ => floorTilemap
        };
    }

    private ShipPartDefinition FindPartByName(string partName)
    {
        if (availableParts == null) return null;
        foreach (var p in availableParts)
            if (p.partName == partName) return p;
        return null;
    }

    #endregion

    #region Utilities

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

    #endregion

    #region ISaveable

    public string SaveKey => $"AirshipBuildMode:{airship?.AirshipId ?? "unknown"}";

    public string SaveState()
    {
        return JsonUtility.ToJson(new AirshipBuildSaveData { tiles = placedTiles.ToArray() });
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<AirshipBuildSaveData>(json);
        if (data?.tiles == null) return;

        placedTiles.Clear();
        foreach (var entry in data.tiles)
        {
            var part = FindPartByName(entry.partName);
            if (part == null || part.tile == null)
            {
                Debug.LogWarning($"[AirshipBuildMode] RestoreState: part '{entry.partName}' not found");
                continue;
            }

            Tilemap map = GetTargetTilemap(entry.layer);
            if (map == null) continue;

            map.SetTile(new Vector3Int(entry.x, entry.y, 0), part.tile);
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
        public string partName;
        public AirshipTilemapLayer layer;
    }

    #endregion
}
