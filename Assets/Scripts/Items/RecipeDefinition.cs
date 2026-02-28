using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Cloudstead/Crafting/Recipe Definition")]
public class RecipeDefinition : ScriptableObject
{
    [Required]
    public string recipeName;

    [ListDrawerSettings(ShowIndexLabels = true)]
    public ItemDefinition[] inputItems;

    [ListDrawerSettings(ShowIndexLabels = true)]
    public int[] inputCounts;

    [Required]
    [PreviewField(64)]
    public ItemDefinition outputItem;

    [Min(1)]
    public int outputCount = 1;

    [Min(0.1f)]
    public float craftTime = 1f;

    [TextArea(1, 3)]
    public string description;

    /// <summary>
    /// Check if inputs are valid (arrays same length, no nulls).
    /// </summary>
    public bool IsValid()
    {
        if (inputItems == null || inputCounts == null) return false;
        if (inputItems.Length != inputCounts.Length) return false;
        if (outputItem == null) return false;
        for (int i = 0; i < inputItems.Length; i++)
        {
            if (inputItems[i] == null || inputCounts[i] <= 0) return false;
        }
        return true;
    }
}
