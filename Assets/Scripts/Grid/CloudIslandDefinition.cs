using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Static identity data for a cloud island.
/// Referenced by both CloudIsland (to get its ID) and CutsceneStep (for cloud targeting).
/// Create assets at Assets/Data/Islands/.
/// </summary>
[CreateAssetMenu(menuName = "Cloudstead/Cloud Island Definition", fileName = "NewCloudIsland")]
public class CloudIslandDefinition : ScriptableObject
{
    [LabelText("Cloud ID"), Required]
    [Tooltip("Unique identifier. Must match the CloudIsland scene component.")]
    public string cloudId;

    [LabelText("Display Name")]
    public string displayName;

    [Header("Size")]
    public int cloudWidth = 30;
    public int cloudHeight = 30;
    public int seed;

    [Header("Generation")]
    [Tooltip("When true, walkability is derived from the existing painted tilemap instead of being procedurally generated. Boundary and resource nodes are still built automatically.")]
    public bool handBuilt;
}
