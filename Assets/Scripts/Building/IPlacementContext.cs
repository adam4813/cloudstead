using UnityEngine;

/// <summary>
/// Strategy interface for context-dependent item placement.
/// Implementations define how items are validated, positioned, parented, and rendered
/// in different environments (cloud surface, building interior, airship deck).
/// </summary>
public interface IPlacementContext
{
    /// <summary>Returns true if an item can be placed at the given grid cell.</summary>
    bool IsValidPosition(Vector3Int cellPos);

    /// <summary>Converts a grid cell to the world position the item should occupy.</summary>
    Vector3 CellToWorldPosition(Vector3Int cellPos);

    /// <summary>Converts a world position to the grid cell it falls in.</summary>
    Vector3Int WorldToCell(Vector3 worldPos);

    /// <summary>The transform items should be parented to (null = scene root).</summary>
    Transform GetParent();

    /// <summary>The sorting layer name to use for placed items and the ghost.</summary>
    string SortingLayer { get; }
}
