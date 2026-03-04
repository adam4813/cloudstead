using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class BuildingInterior : MonoBehaviour
{
    [FoldoutGroup("Identity")]
    [SerializeField] private string buildingId;
    public string BuildingId => buildingId;

    [FoldoutGroup("Tilemaps")]
    [Tooltip("All interior tilemap layers (floor, walls, decoration). Colliders on these tilemaps enable/disable automatically.")]
    [SerializeField] private Tilemap[] interiorTilemaps;

    [FoldoutGroup("Tilemaps")]
    [Tooltip("Large solid sprite that covers the exterior world when inside. Assign a child SpriteRenderer sized to fill the camera view.")]
    [SerializeField] private SpriteRenderer backdropRenderer;

    [FoldoutGroup("Spawns")]
    [Required] [SerializeField] private Transform interiorSpawnPoint;
    public Transform InteriorSpawnPoint => interiorSpawnPoint;

    [FoldoutGroup("Spawns")]
    [Required] [SerializeField] private Transform exteriorSpawnPoint;
    public Transform ExteriorSpawnPoint => exteriorSpawnPoint;

    [FoldoutGroup("Objects")]
    [Tooltip("Parent transform for all interior objects (doors, furniture, placed items). Toggled with visibility.")]
    [SerializeField] private Transform objectsContainer;
    public Transform ObjectsContainer => objectsContainer;

    [FoldoutGroup("Objects")]
    [Tooltip("Container whose children with PlacedItem components are auto-registered on start")]
    [SerializeField] private Transform placeableObjectsContainer;
    public Transform PlaceableObjectsContainer => placeableObjectsContainer;

    [FoldoutGroup("Lighting")]
    [SerializeField] private Light2D interiorAmbientLight;
    public Light2D InteriorAmbientLight => interiorAmbientLight;

    private bool _itemsRegistered;

    private void Awake() => SetVisible(false);

    public void SetVisible(bool visible)
    {
        if (backdropRenderer != null)
            backdropRenderer.enabled = visible;

        foreach (var tm in interiorTilemaps)
            if (tm != null) tm.gameObject.SetActive(visible);

        if (objectsContainer != null)
            objectsContainer.gameObject.SetActive(visible);

        // Toggle separately in case placeableObjectsContainer is not under objectsContainer
        if (placeableObjectsContainer != null && placeableObjectsContainer != objectsContainer
            && (objectsContainer == null || !placeableObjectsContainer.IsChildOf(objectsContainer)))
            placeableObjectsContainer.gameObject.SetActive(visible);

        if (interiorAmbientLight != null)
            interiorAmbientLight.enabled = visible;

        if (visible && !_itemsRegistered)
        {
            _itemsRegistered = true;
            PlacementManager.Instance?.RegisterExistingItems(placeableObjectsContainer);
        }
    }

    /// <summary>Returns true if any interior tilemap has a tile at this world-grid position (used for walkability).</summary>
    public bool HasFloorTile(Vector3Int worldGridPos)
    {
        Vector3 worldPos = new Vector3(worldGridPos.x + 0.5f, worldGridPos.y + 0.5f, 0f);
        foreach (var tm in interiorTilemaps)
        {
            if (tm == null) continue;
            Vector3Int cellPos = tm.WorldToCell(worldPos);
            if (tm.HasTile(cellPos)) return true;
        }
        return false;
    }

    /// <summary>World-space bounds computed from all interior tilemaps. Used by InteriorCamera for clamping.</summary>
    public Bounds GetBounds()
    {
        var bounds = new Bounds();
        bool first = true;
        foreach (var tm in interiorTilemaps)
        {
            if (tm == null) continue;
            tm.CompressBounds();
            var b = tm.localBounds;
            // Convert tilemap local bounds to world space
            var worldMin = tm.transform.TransformPoint(b.min);
            var worldMax = tm.transform.TransformPoint(b.max);
            var worldBounds = new Bounds();
            worldBounds.SetMinMax(worldMin, worldMax);
            if (first) { bounds = worldBounds; first = false; }
            else bounds.Encapsulate(worldBounds);
        }
        return bounds;
    }
}
