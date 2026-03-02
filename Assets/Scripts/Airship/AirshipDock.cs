using UnityEngine;

/// <summary>
/// Marks the fixed mooring point on the cloud edge where the airship parks.
/// Requires a LandingPad so AirshipController.OnLand() can detect this as
/// a dock (re-moors to Kinematic) vs an open-sky pad (stays Dynamic).
///
/// The player boards via AirshipHelm on the airship itself, not this dock.
///
/// Future: implement IBuildable so the dock can be placed/moved on edge tiles.
/// </summary>
[RequireComponent(typeof(LandingPad))]
public class AirshipDock : MonoBehaviour
{
}

