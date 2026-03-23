using UnityEngine.Tilemaps;
using UnityEngine;

/// <summary>
/// Abstraction for any environment that supports farming operations (tilling, watering, planting).
/// Implemented by CloudIsland, BuildingInterior (greenhouses), and AirshipController (deck flowerbeds).
/// </summary>
public interface IFarmingContext
{
    /// <summary>Unique identifier for save-data scoping (e.g. "cloud:home", "interior:greenhouse_01").</summary>
    string ContextId { get; }

    /// <summary>Runtime soil tilemap for tilled/watered tile visuals. Null if this context doesn't support farming.</summary>
    Tilemap SoilTilemap { get; }

    /// <summary>Returns true if the given tile position is a valid farming target in this context.</summary>
    bool IsWalkable(Vector3Int tilePos);
}
