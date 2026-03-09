using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small corner mini-map visible during airship flight.
/// Shows island dots and player position, centered on the player.
/// Toggle with the N key or show/hide based on game state.
/// </summary>
public class MiniMapUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject miniMapPanel;
    [SerializeField] private RectTransform mapContainer;
    [SerializeField] private RectTransform mapMask;

    [Header("Prefabs")]
    [SerializeField] private GameObject islandDotPrefab;

    [Header("Player Marker")]
    [SerializeField] private RectTransform playerMarker;

    [Header("Waypoint Marker")]
    [SerializeField] private RectTransform waypointMarker;

    [Header("Settings")]
    [SerializeField] private float mapScale = 1.5f;
    [SerializeField] private int refreshInterval = 10;

    private readonly List<GameObject> _dots = new();
    private int _frameCounter;
    private bool _manuallyHidden;

    private void OnEnable()
    {
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Subscribe<IslandDiscoveredEvent>(OnIslandDiscovered);
        EventBus.Subscribe<WaypointSetEvent>(OnWaypointSet);
        EventBus.Subscribe<WaypointClearedEvent>(OnWaypointCleared);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Unsubscribe<IslandDiscoveredEvent>(OnIslandDiscovered);
        EventBus.Unsubscribe<WaypointSetEvent>(OnWaypointSet);
        EventBus.Unsubscribe<WaypointClearedEvent>(OnWaypointCleared);
    }

    private void Start()
    {
        SetVisible(false);
        if (waypointMarker != null) waypointMarker.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Toggle with N key
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.nKey.wasPressedThisFrame)
        {
            _manuallyHidden = !_manuallyHidden;
            UpdateVisibility();
        }

        if (miniMapPanel == null || !miniMapPanel.activeSelf) return;

        _frameCounter++;
        if (_frameCounter >= refreshInterval)
        {
            _frameCounter = 0;
            RefreshDots();
        }

        UpdatePlayerMarker();
        UpdateWaypointMarker();
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        UpdateVisibility();
        if (evt.Current == GameState.Airship)
        {
            _frameCounter = refreshInterval; // force refresh on next frame
        }
    }

    private void OnIslandDiscovered(IslandDiscoveredEvent evt)
    {
        _frameCounter = refreshInterval;
    }

    private void OnWaypointSet(WaypointSetEvent evt)
    {
        if (waypointMarker != null) waypointMarker.gameObject.SetActive(true);
    }

    private void OnWaypointCleared(WaypointClearedEvent evt)
    {
        if (waypointMarker != null) waypointMarker.gameObject.SetActive(false);
    }

    private void UpdateVisibility()
    {
        var gm = GameManager.Instance;
        bool shouldShow = gm != null && gm.CurrentState == GameState.Airship && !_manuallyHidden;
        SetVisible(shouldShow);
    }

    private void SetVisible(bool visible)
    {
        if (miniMapPanel != null)
            miniMapPanel.SetActive(visible);
    }

    private void RefreshDots()
    {
        if (IslandRegistry.Instance == null || mapContainer == null) return;

        // Clear old dots
        foreach (var dot in _dots)
            if (dot != null) Destroy(dot);
        _dots.Clear();

        var islands = IslandRegistry.Instance.Islands;
        foreach (var island in islands)
        {
            if (islandDotPrefab == null) break;

            var dot = Instantiate(islandDotPrefab, mapContainer);
            _dots.Add(dot);

            // Size based on island radius
            var rt = dot.GetComponent<RectTransform>();
            float size = Mathf.Max(8f, island.approximateRadius * mapScale * 2f);
            rt.sizeDelta = new Vector2(size, size);

            // Color
            var img = dot.GetComponent<Image>();
            if (img != null)
                img.color = island.isHome ? new Color(0.5f, 0.85f, 0.5f, 0.9f) : new Color(0.7f, 0.8f, 1f, 0.9f);

            // Label
            var label = dot.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = island.displayName;
                label.fontSize = 8f;
            }

            // Store island data for position updates
            dot.name = island.cloudId;
        }
    }

    private void UpdatePlayerMarker()
    {
        if (playerMarker == null || mapContainer == null) return;

        var player = GetPlayerWorldPos(out bool valid);
        if (!valid) return;

        // Player is always at center — move dots relative to player
        playerMarker.anchoredPosition = Vector2.zero;

        // Rotate player marker to show heading
        var airship = FindAirship();
        if (airship != null)
            playerMarker.rotation = Quaternion.Euler(0, 0, airship.transform.eulerAngles.z);

        // Position all island dots relative to player
        if (IslandRegistry.Instance == null) return;
        var islands = IslandRegistry.Instance.Islands;

        int dotIdx = 0;
        foreach (var island in islands)
        {
            if (dotIdx >= _dots.Count) break;
            var dot = _dots[dotIdx];
            if (dot == null) { dotIdx++; continue; }

            var rt = dot.GetComponent<RectTransform>();
            Vector2 offset = (island.worldCenter - player) * mapScale;
            rt.anchoredPosition = offset;
            dotIdx++;
        }
    }

    private void UpdateWaypointMarker()
    {
        if (waypointMarker == null) return;

        var wm = WaypointManager.Instance;
        if (wm == null || !wm.HasWaypoint) return;

        var player = GetPlayerWorldPos(out bool valid);
        if (!valid) return;

        Vector2 offset = (wm.CurrentWaypoint.Value.worldCenter - player) * mapScale;
        waypointMarker.anchoredPosition = offset;
    }

    private Vector2 GetPlayerWorldPos(out bool valid)
    {
        // Prefer airship position when flying
        var airship = FindAirship();
        if (airship != null)
        {
            valid = true;
            return airship.transform.position;
        }

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            valid = true;
            return pc.transform.position;
        }

        valid = false;
        return Vector2.zero;
    }

    private AirshipController FindAirship()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null && pc.CurrentAirship != null)
            return pc.CurrentAirship;
        return null;
    }
}
