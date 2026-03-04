using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple sky map UI showing all registered islands as labeled circles.
/// Toggle via M key or HUD button. View-only — no click-to-fly.
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

    private bool _isOpen;

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
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

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

        GameManager.Instance?.SetState(GameState.Playing);
    }

    private void RefreshMap()
    {
        if (IslandRegistry.Instance == null || islandContainer == null) return;

        // Clear old dots
        for (int i = islandContainer.childCount - 1; i >= 0; i--)
            Destroy(islandContainer.GetChild(i).gameObject);

        var islands = IslandRegistry.Instance.Islands;
        var home = IslandRegistry.Instance.GetHome();
        Vector2 homeCenter = home.HasValue ? home.Value.worldCenter : Vector2.zero;

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

            // Home gets a distinct color
            var img = dot.GetComponent<Image>();
            if (img != null)
                img.color = island.isHome ? new Color(0.5f, 0.85f, 0.5f, 0.8f) : new Color(0.7f, 0.8f, 1f, 0.8f);
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
}
