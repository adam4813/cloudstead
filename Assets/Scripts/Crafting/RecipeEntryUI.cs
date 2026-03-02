using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeEntryUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI recipeName;
    [SerializeField] private TextMeshProUGUI ingredients;
    [SerializeField] private Button craftButton;

    private RecipeDefinition _recipe;
    private CraftingUI _owner;

    public void Setup(RecipeDefinition recipe, CraftingUI owner)
    {
        _recipe = recipe;
        _owner = owner;

        if (icon != null && recipe.outputItem?.icon != null)
            icon.sprite = recipe.outputItem.icon;

        if (recipeName != null)
            recipeName.text = recipe.recipeName;

        if (ingredients != null)
            ingredients.text = BuildIngredientsText(recipe);

        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(OnCraftClicked);
        }

        // Select this recipe when the row is clicked
        var selectButton = GetComponent<Button>();
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelected);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (craftButton == null || _recipe == null) return;
        craftButton.interactable = CraftingManager.Instance != null && CraftingManager.Instance.CanCraft(_recipe);
    }

    private void OnSelected()
    {
        _owner?.OnRecipeSelected(_recipe);
    }

    private void OnCraftClicked()
    {
        _owner?.OnRecipeSelected(_recipe);
        _owner?.OnCraftButton();
    }

    private static string BuildIngredientsText(RecipeDefinition recipe)
    {
        if (recipe.inputItems == null || recipe.inputItems.Length == 0)
            return "No ingredients";

        var sb = new StringBuilder();
        for (int i = 0; i < recipe.inputItems.Length; i++)
        {
            if (recipe.inputItems[i] == null) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append($"{recipe.inputItems[i].itemName} x{recipe.inputCounts[i]}");
        }
        return sb.ToString();
    }
}
