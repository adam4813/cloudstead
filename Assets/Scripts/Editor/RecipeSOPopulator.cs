using UnityEditor;
using UnityEngine;

public static class RecipeSOPopulator
{
    [MenuItem("Cloudstead/Populate Starter Recipes")]
    private static void PopulateStarterRecipes()
    {
        int created = 0;
        int skipped = 0;

        // ── Processed output items (needed as recipe outputs) ─────────────────
        EnsureFolderExists("Assets/Data/Items/Processed");

        var processedItems = new (string name, string desc, int sell, int stamina, ItemCategory cat)[]
        {
            ("Plank",          "Smooth timber, ready for building.",         5,   0,  ItemCategory.CraftingMaterial),
            ("Fiber Cloth",    "Woven from plant fibers.",                   8,   0,  ItemCategory.CraftingMaterial),
            ("Stone Brick",    "Hewn and squared for construction.",         10,  0,  ItemCategory.CraftingMaterial),
            ("Torch",          "A coal-tipped torch that wards off the dark.", 4, 0,  ItemCategory.General),
            ("Cooked Turnip",  "Roasted turnip, warm and filling.",          40,  20, ItemCategory.General),
            ("Baked Potato",   "A hearty baked potato.",                     100, 40, ItemCategory.General),
            ("Strawberry Jam", "Sweet jam from sun-ripened berries.",        150, 60, ItemCategory.General),
        };

        foreach (var (iName, desc, sell, stamina, cat) in processedItems)
        {
            var safeName = iName.Replace(" ", "");
            var path = $"Assets/Data/Items/Processed/{safeName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(path) != null)
            {
                skipped++;
                continue;
            }

            var asset = ScriptableObject.CreateInstance<ItemDefinition>();
            asset.itemName       = iName;
            asset.description    = desc;
            asset.sellPrice      = sell;
            asset.buyPrice       = 0;
            asset.category       = cat;
            asset.staminaRestore = stamina;
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[RecipeSOPopulator] Created item: {path}");
            created++;
        }

        // Save items before creating recipes (recipes reference them)
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── Recipe SOs ────────────────────────────────────────────────────────
        EnsureFolderExists("Assets/Data/Recipes");

        var recipes = new (
            string recipeName,
            string[] inputNames,
            int[]    inputCounts,
            string   outputName,
            int      outputCount,
            float    craftTime,
            string   desc
        )[]
        {
            ("Craft Planks",        new[]{"Wood"},        new[]{2},   "Plank",          2, 2f, "Shape rough wood into usable planks."),
            ("Weave Cloth",         new[]{"Fiber"},       new[]{4},   "Fiber Cloth",     1, 3f, "Weave plant fibers into sturdy cloth."),
            ("Cut Stone Brick",     new[]{"Stone"},       new[]{2},   "Stone Brick",     1, 2f, "Cut rough stone into neat bricks."),
            ("Craft Torch",         new[]{"Wood","Coal"}, new[]{1,1}, "Torch",           2, 1f, "Wrap coal around wood for a torch."),
            ("Cook Turnip",         new[]{"Turnip"},      new[]{1},   "Cooked Turnip",   1, 2f, "Roast a turnip over the fire."),
            ("Bake Potato",         new[]{"Potato"},      new[]{1},   "Baked Potato",    1, 3f, "Slowly bake a potato until fluffy."),
            ("Make Strawberry Jam", new[]{"Strawberry"},  new[]{2},   "Strawberry Jam",  1, 4f, "Simmer strawberries into sweet jam."),
        };

        foreach (var r in recipes)
        {
            var safe = r.recipeName.Replace(" ", "");
            var path = $"Assets/Data/Recipes/{safe}.asset";
            if (AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path) != null)
            {
                Debug.Log($"[RecipeSOPopulator] Skipped existing: {path}");
                skipped++;
                continue;
            }

            var inputs  = new ItemDefinition[r.inputNames.Length];
            bool valid  = true;
            for (int i = 0; i < r.inputNames.Length; i++)
            {
                inputs[i] = FindItem(r.inputNames[i]);
                if (inputs[i] == null) { valid = false; break; }
            }
            var output = FindItem(r.outputName);
            if (output == null) valid = false;

            if (!valid)
            {
                Debug.LogWarning($"[RecipeSOPopulator] Skipping '{r.recipeName}' — missing item reference. Run Populate Starter Items first.");
                skipped++;
                continue;
            }

            var recipe         = ScriptableObject.CreateInstance<RecipeDefinition>();
            recipe.recipeName  = r.recipeName;
            recipe.inputItems  = inputs;
            recipe.inputCounts = r.inputCounts;
            recipe.outputItem  = output;
            recipe.outputCount = r.outputCount;
            recipe.craftTime   = r.craftTime;
            recipe.description = r.desc;

            AssetDatabase.CreateAsset(recipe, path);
            Debug.Log($"[RecipeSOPopulator] Created recipe: {path}");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Populate Starter Recipes",
            $"Done!\nCreated: {created}\nSkipped: {skipped}\n\nRemember to assign the recipe SOs to CraftingManager.allRecipes in the Inspector.",
            "OK");
    }

    private static ItemDefinition FindItem(string itemName)
    {
        var safeName = itemName.Replace(" ", "");
        string[] folders = {
            "Assets/Data/Items/Materials",
            "Assets/Data/Items/Crops",
            "Assets/Data/Items/Seeds",
            "Assets/Data/Items/Processed",
        };
        foreach (var folder in folders)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{folder}/{safeName}.asset");
            if (item != null) return item;
        }
        Debug.LogWarning($"[RecipeSOPopulator] ItemDefinition not found for '{itemName}'");
        return null;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        var parts   = folderPath.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
