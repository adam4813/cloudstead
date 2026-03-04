using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// A named tile condition tag. Each asset represents one condition
/// (e.g. "Walkable", "Non-Edge", "Has Farm Plot"). Tools and items
/// reference these via lists on their ScriptableObject definitions.
/// The TileConditionRegistry maps each tag to its evaluation function.
/// Modders can create new tags and register custom evaluators at runtime.
/// </summary>
[CreateAssetMenu(fileName = "NewTileCondition", menuName = "Cloudstead/Tile Condition Tag")]
public class TileConditionTag : ScriptableObject
{
    [Required]
    [Tooltip("Human-readable label shown in inspector dropdowns")]
    public string displayName;

    [TextArea(1, 3)]
    [Tooltip("What this condition checks for")]
    public string description;

    /// <summary>Unique identifier for this condition. Uses the SO asset name.</summary>
    public string TagId => name;
}
