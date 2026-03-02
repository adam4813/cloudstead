using UnityEngine;

public class CraftingManager : Singleton<CraftingManager>
{
    [SerializeField] private RecipeDefinition[] allRecipes;

    public override void Initialize()
    {
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        base.OnDestroy();
    }

    private void OnInventoryChanged(InventoryChangedEvent evt)
    {
        // CraftingUI subscribes directly to InventoryChangedEvent — nothing to do here.
        // This subscription exists only to keep CraftingManager's internal state current
        // if future logic needs to react to inventory changes (e.g. unlock new recipes).
    }

    public bool CanCraft(RecipeDefinition recipe)
    {
        if (recipe == null || !recipe.IsValid()) return false;
        if (InventoryManager.Instance == null) return false;

        for (int i = 0; i < recipe.inputItems.Length; i++)
        {
            if (!InventoryManager.Instance.HasItem(recipe.inputItems[i], recipe.inputCounts[i]))
                return false;
        }
        return true;
    }

    public bool Craft(RecipeDefinition recipe)
    {
        if (!CanCraft(recipe)) return false;

        for (int i = 0; i < recipe.inputItems.Length; i++)
            InventoryManager.Instance.RemoveItem(recipe.inputItems[i], recipe.inputCounts[i]);

        InventoryManager.Instance.AddItem(recipe.outputItem, recipe.outputCount);
        EventBus.Publish(new ItemCraftedEvent { Recipe = recipe });
        return true;
    }

    public RecipeDefinition[] GetCraftableRecipes()
    {
        if (allRecipes == null) return System.Array.Empty<RecipeDefinition>();

        var result = new System.Collections.Generic.List<RecipeDefinition>();
        foreach (var recipe in allRecipes)
        {
            if (CanCraft(recipe))
                result.Add(recipe);
        }
        return result.ToArray();
    }

    public RecipeDefinition[] GetAllRecipes()
    {
        return allRecipes ?? System.Array.Empty<RecipeDefinition>();
    }
}
