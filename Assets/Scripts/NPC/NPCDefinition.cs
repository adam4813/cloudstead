using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewNPC", menuName = "Cloudstead/NPC/NPC Definition")]
public class NPCDefinition : ScriptableObject
{
    [FoldoutGroup("Identity")]
    [Required]
    public string npcName;

    [FoldoutGroup("Identity")]
    [PreviewField(64)]
    public Sprite portrait;

    [FoldoutGroup("Dialogue")]
    [TextArea(1, 3)]
    public string[] greetings;

    [FoldoutGroup("Dialogue")]
    [TextArea(1, 3)]
    public string[] farewells;

    [FoldoutGroup("Shop")]
    public bool isMerchant;

    [FoldoutGroup("Shop")]
    [ShowIf("isMerchant")]
    public ItemDefinition[] shopInventory;

    [FoldoutGroup("Shop")]
    [ShowIf("isMerchant")]
    [Tooltip("Override prices. If empty or shorter than shopInventory, falls back to item.buyPrice")]
    public int[] shopPrices;

    public int GetShopPrice(int index)
    {
        if (shopPrices != null && index < shopPrices.Length && shopPrices[index] > 0)
            return shopPrices[index];
        if (shopInventory != null && index < shopInventory.Length && shopInventory[index] != null)
            return shopInventory[index].buyPrice;
        return 0;
    }
}
