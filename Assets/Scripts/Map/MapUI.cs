using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Sky map UI showing all registered islands as labeled circles.
/// Toggle via M key or HUD button. Click an island dot to set it as a waypoint.
/// </summary>
public class MapUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject mapPanel;

    [Header("Rendering")]
    [SerializeField] private RectTransform islandContainer;
    [SerializeField] private GameObject islandDotPrefab;
    [SerializeField] private float mapScale = 2f;

    [Header("Player Marker")]
    [SerializeField] private RectTransform playerMarker;

    [Header("Waypoint")]
    [SerializeField] private Color waypointHighlightColor = new(1f, 0.85f, 0.3f, 1f);

    private bool _isOpen;
    private readonly List<(GameObject dot, IslandInfo island)> _dotEntries = new();

    private void Start()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (_isOpen) return;
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentState != GameState.Playing && gm.CurrentState != GameState.Airship) return;

        _isOpen = true;
        if (mapPanel != null)
            mapPanel.SetActive(true);

        GameManager.Instance?.SetState(GameState.Menu);
        RefreshMap();
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (mapPanel != null)
            mapPanel.SetActive(false);

        //GameManager.Instance?.RestorePreviousState();
        GameManager.Instance?.SetState(GameState.Playing);
    }

    private void RefreshMap()
    {
        if (IslandRegistry.Instance == null || islandContainer == null) return;

        // Clear old dots
        foreach (var entry in _dotEntries)
            if (entry.dot != null) Destroy(entry.dot);
        _dotEntries.Clear();

        var islands = IslandRegistry.Instance.Islands;
        var home = IslandRegistry.Instance.GetHome();
        Vector2 homeCenter = home.HasValue ? home.Value.worldCenter : Vector2.zero;

        string activeWaypointId = WaypointManager.Instance != null && WaypointManager.Instance.HasWaypoint
            ? WaypointManager.Instance.CurrentWaypoint.Value.cloudId
            : null;

        foreach (var island in islands)
        {
            if (islandDotPrefab == null) break;

            var dot = Instantiate(islandDotPrefab, islandContainer);
            var rt = dot.GetComponent<RectTransform>();

            // Position relative to home
            Vector2 offset = (island.worldCenter - homeCenter) * mapScale;
            rt.anchoredPosition = offset;

            // Scale circle by island radius
            float size = Mathf.Max(20f, island.approximateRadius * mapScale * 2f);
            rt.sizeDelta = new Vector2(size, size);

            // Label
            var label = dot.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = island.displayName;

            // Color — highlight active waypoint, otherwise home green / default blue
            var img = dot.GetComponent<Image>();
            if (img != null)
            {
                if (island.cloudId == activeWaypointId)
                    img.color = waypointHighlightColor;
                else if (island.isHome)
                    img.color = new Color(0.5f, 0.85f, 0.5f, 0.8f);
                else
                    img.color = new Color(0.7f, 0.8f, 1f, 0.8f);
            }

            // Make clickable for waypoint setting
            var btn = dot.GetComponent<Button>();
            if (btn == null)
                btn = dot.AddComponent<Button>();

            var captured = island;
            btn.onClick.AddListener(() => OnIslandClicked(captured));

            _dotEntries.Add((dot, island));
        }

        // Update player marker
        if (playerMarker != null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                Vector2 playerOffset = ((Vector2)player.transform.position - homeCenter) * mapScale;
                playerMarker.anchoredPosition = playerOffset;
            }
        }
    }

    private void OnIslandClicked(IslandInfo island)
    {
        if (WaypointManager.Instance == null) return;

        // Toggle waypoint: click same island again to clear
        if (WaypointManager.Instance.HasWaypoint &&
            WaypointManager.Instance.CurrentWaypoint.Value.cloudId == island.cloudId)
        {
            WaypointManager.Instance.ClearWaypoint();
        }
        else
        {
            WaypointManager.Instance.SetWaypoint(island);
        }

        // Refresh to update highlight colors
        RefreshMap();
    }
}
