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

    [FoldoutGroup("Respawn")] public bool destroyOnDepletion = true;
    [FoldoutGroup("Respawn")] [ShowIf("@!destroyOnDepletion")] [MinValue(1)] public int respawnDaysMin = 3;
    [FoldoutGroup("Respawn")] [ShowIf("@!destroyOnDepletion")] [MinValue(1)] public int respawnDaysMax = 7;
}
