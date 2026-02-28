using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewTool", menuName = "Cloudstead/Tools/Tool Definition")]
public class ToolDefinition : ItemDefinition
{
    [FoldoutGroup("Tool Settings")]
    public ToolType toolType;

    [FoldoutGroup("Tool Settings")]
    [Min(0)]
    public int staminaCost = 2;

    [FoldoutGroup("Tool Settings")]
    [Min(0.1f)]
    public float useTime = 0.3f;

    [FoldoutGroup("Tool Settings")]
    [Min(1)]
    public int tier = 1;
}
