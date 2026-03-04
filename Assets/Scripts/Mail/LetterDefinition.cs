using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLetter", menuName = "Cloudstead/Mail/Letter Definition")]
public class LetterDefinition : ScriptableObject
{
    [Required]
    public string sender;

    public string subject;

    [TextArea(3, 10)]
    public string body;

    [Header("Delivery Schedule")]
    [Tooltip("Game day to deliver (0 = deliver immediately on game start)")]
    public int deliveryDay;

    [Tooltip("Season to deliver in (-1 or unset = any season)")]
    public Season deliverySeason = (Season)(-1);

    [Header("Attachment")]
    [Tooltip("Optional item to attach to the letter")]
    public ItemDefinition attachedItem;

    public int attachedItemCount = 1;
}
