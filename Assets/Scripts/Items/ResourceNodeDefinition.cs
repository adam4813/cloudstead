using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Cloudstead/Items/Resource Node Definition", fileName = "NewResourceNode")]
public class ResourceNodeDefinition : ScriptableObject
{
    [FoldoutGroup("Identity")] public string nodeName;

    [FoldoutGroup("Drops")] [Required] public ItemDefinition dropItem;
    [FoldoutGroup("Drops")] [MinValue(1)] public int dropCountMin = 1;
    [FoldoutGroup("Drops")] [MinValue(1)] public int dropCountMax = 1;

    [FoldoutGroup("Interaction")] [MinValue(1)] public int maxHits = 3;
    [FoldoutGroup("Interaction")] public Sprite[] hitSprites;

    [FoldoutGroup("Audio")] public AudioClip hitSound;
    [FoldoutGroup("Audio")] public AudioClip depletedSound;

    [FoldoutGroup("Respawn")] public bool spawnsNaturally = true;
    [FoldoutGroup("Respawn")] [ShowIf("spawnsNaturally")] [MinValue(1)] public int spawnIntervalMin = 3;
    [FoldoutGroup("Respawn")] [ShowIf("spawnsNaturally")] [MinValue(1)] public int spawnIntervalMax = 7;
    [FoldoutGroup("Respawn")] [ShowIf("spawnsNaturally")] [MinValue(1)] public int spawnCountMin = 1;
    [FoldoutGroup("Respawn")] [ShowIf("spawnsNaturally")] [MinValue(1)] public int spawnCountMax = 2;
}
