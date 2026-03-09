using UnityEngine;

/// <summary>
/// Holds the active navigation waypoint. Other systems (WaypointArrow, MiniMapUI)
/// subscribe to WaypointSetEvent / WaypointClearedEvent to react.
/// </summary>
public class WaypointManager : Singleton<WaypointManager>
{
    public IslandInfo? CurrentWaypoint { get; private set; }
    public bool HasWaypoint => CurrentWaypoint.HasValue;

    public override void Initialize() { }

    public void SetWaypoint(IslandInfo target)
    {
        CurrentWaypoint = target;
        EventBus.Publish(new WaypointSetEvent { Target = target });
    }

    public void ClearWaypoint()
    {
        if (!HasWaypoint) return;
        CurrentWaypoint = null;
        EventBus.Publish(new WaypointClearedEvent());
    }

    /// <summary>
    /// Auto-clear waypoint when the player lands on the target island.
    /// </summary>
    private void OnEnable()
    {
        EventBus.Subscribe<AirshipLandedEvent>(OnLanded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AirshipLandedEvent>(OnLanded);
    }

    private void OnLanded(AirshipLandedEvent evt)
    {
        if (!HasWaypoint) return;

        // Clear if we landed on or near the target island
        if (CurrentWaypoint.Value.displayName == evt.CloudName ||
            CurrentWaypoint.Value.cloudId == evt.CloudName)
        {
            ClearWaypoint();
        }
    }
}
