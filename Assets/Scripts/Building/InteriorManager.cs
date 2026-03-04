using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class InteriorManager : Singleton<InteriorManager>
{
    [FoldoutGroup("Settings")]
    [Tooltip("Pause game time when a player enters an interior. Disable for multiplayer.")]
    [SerializeField] private bool pauseTimeOnEnter = true;

    [FoldoutGroup("Layers")]
    [Tooltip("Physics layer for exterior entities.")]
    [SerializeField] private string exteriorPhysicsLayer = "Player";

    [FoldoutGroup("Layers")]
    [Tooltip("Physics layer for interior entities.")]
    [SerializeField] private string interiorPhysicsLayer = "InteriorSpace";

    // Exterior → Interior sorting layer mapping
    private static readonly Dictionary<string, string> _toInterior = new()
    {
        { "Ground",     "InteriorGround" },
        { "GroundDecor","InteriorGroundDecor" },
        { "Objects",    "InteriorObjects" },
        { "Characters", "InteriorCharacters" },
    };

    private static readonly Dictionary<string, string> _toExterior = new()
    {
        { "InteriorGround",     "Ground" },
        { "InteriorGroundDecor","GroundDecor" },
        { "InteriorObjects",    "Objects" },
        { "InteriorCharacters", "Characters" },
    };

    private BuildingInterior _currentInterior;
    private GameObject _player;

    public bool IsInsideInterior => _currentInterior != null;
    public BuildingInterior CurrentInterior => _currentInterior;

    /// <summary>Maps an exterior sorting layer to its interior counterpart.</summary>
    public static string ToInteriorLayer(string exteriorLayer)
        => _toInterior.TryGetValue(exteriorLayer, out var v) ? v : exteriorLayer;

    /// <summary>Maps an interior sorting layer to its exterior counterpart.</summary>
    public static string ToExteriorLayer(string interiorLayer)
        => _toExterior.TryGetValue(interiorLayer, out var v) ? v : interiorLayer;

    public override void Initialize()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    /// <summary>Switches a GameObject's physics layer and all SpriteRenderers to the target space.
    /// Each SpriteRenderer's sorting layer is mapped to its counterpart; sortingOrder is preserved.</summary>
    public void SetEntitySpace(GameObject entity, bool interior)
    {
        if (entity == null) return;

        string physicsLayer = interior ? interiorPhysicsLayer : exteriorPhysicsLayer;
        int layerIndex = LayerMask.NameToLayer(physicsLayer);
        if (layerIndex >= 0)
            entity.layer = layerIndex;

        var map = interior ? _toInterior : _toExterior;
        foreach (var sr in entity.GetComponentsInChildren<SpriteRenderer>())
        {
            if (map.TryGetValue(sr.sortingLayerName, out var mapped))
                sr.sortingLayerName = mapped;
        }
    }

    public void EnterInterior(BuildingInterior interior, Vector3? positionOverride = null)
    {
        if (interior == null || _currentInterior == interior) return;

        interior.SetVisible(true);
        _currentInterior = interior;

        PlacementManager.Instance?.SetContext(new InteriorPlacementContext(interior));

        if (_player != null)
        {
            if (positionOverride.HasValue)
                _player.transform.position = positionOverride.Value;
            else if (interior.InteriorSpawnPoint != null)
                _player.transform.position = interior.InteriorSpawnPoint.position;
            SetEntitySpace(_player, true);

            var pc = _player.GetComponent<PlayerController>();
            if (pc != null) pc.CurrentInterior = interior;
        }

        if (pauseTimeOnEnter)
            TimeManager.Instance?.PauseTime();

        EventBus.Publish(new InteriorEnteredEvent { BuildingId = interior.BuildingId });
    }

    public void ExitInterior()
    {
        if (_currentInterior == null) return;

        var exitingFrom = _currentInterior;

        PlacementManager.Instance?.SetContext(null);

        if (_player != null)
        {
            if (exitingFrom.ExteriorSpawnPoint != null)
                _player.transform.position = exitingFrom.ExteriorSpawnPoint.position;
            SetEntitySpace(_player, false);

            var pc = _player.GetComponent<PlayerController>();
            if (pc != null) pc.CurrentInterior = null;
        }

        exitingFrom.SetVisible(false);
        _currentInterior = null;

        if (pauseTimeOnEnter)
            TimeManager.Instance?.ResumeTime();

        EventBus.Publish(new InteriorExitedEvent { BuildingId = exitingFrom.BuildingId });
    }
}
