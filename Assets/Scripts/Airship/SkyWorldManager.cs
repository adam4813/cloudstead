using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks all LandingPads in the scene and provides nearest-pad lookup
/// for the airship's Land action.
///
/// LandingPads self-register via RegisterPad() in their Start().
/// No inspector array needed.
///
/// Coordination point for future sky features:
/// - Discovered islands (fog of war)
/// - Sky weather effects
/// - Cloud drift / dynamic positioning
/// - Multiplayer: other players' clouds appearing as new pads
/// </summary>
public class SkyWorldManager : Singleton<SkyWorldManager>
{
    private readonly List<LandingPad> pads = new();

    public override void Initialize()
    {
        // Pads self-register in Start() — no explicit init needed.
    }

    public void RegisterPad(LandingPad pad)
    {
        if (pad != null && !pads.Contains(pad))
            pads.Add(pad);
    }

    public void UnregisterPad(LandingPad pad)
    {
        pads.Remove(pad);
    }

    /// <summary>
    /// Returns the nearest LandingPad that the airship can land at,
    /// or null if none are in range.
    /// </summary>
    public LandingPad GetNearestLandablePad(Vector2 airshipPos)
    {
        LandingPad nearest = null;
        float bestDist = float.MaxValue;

        foreach (var pad in pads)
        {
            if (pad == null) continue;
            if (!pad.CanLand(airshipPos)) continue;

            float dist = Vector2.Distance(airshipPos, (Vector2)pad.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = pad;
            }
        }

        return nearest;
    }
}
