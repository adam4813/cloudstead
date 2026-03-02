using UnityEditor;
using UnityEngine;

public static class ItemSOPopulator
{
    [MenuItem("Cloudstead/Populate Starter Items")]
    private static void PopulateStarterItems()
    {
        int created = 0;
        int skipped = 0;

        // ── Materials ────────────────────────────────────────────────────────
        EnsureFolderExists("Assets/Data/Items/Materials");

        var materials = new (string name, string desc, int sell)[]
        {
            ("Wood",  "Chopped from trees.",        2),
            ("Stone", "Mined from rocks.",          3),
            ("Coal",  "Used for smelting.",         5),
            ("Fiber", "Gathered from wild plants.", 1),
            ("Clay",  "Scooped from muddy earth.",  4),
        };

        foreach (var (itemName, desc, sell) in materials)
        {
            var path = $"Assets/Data/Items/Materials/{itemName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(path) != null)
            {
                Debug.Log($"[ItemSOPopulator] Skipped existing: {path}");
                skipped++;
                continue;
            }

            var asset = ScriptableObject.CreateInstance<ItemDefinition>();
            asset.itemName = itemName;
            asset.description = desc;
            asset.sellPrice = sell;
            asset.buyPrice = sell * 3;
            asset.category = ItemCategory.CraftingMaterial;
            asset.staminaRestore = 0;

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[ItemSOPopulator] Created: {path}");
            created++;
        }

        // ── Crops ────────────────────────────────────────────────────────────
        EnsureFolderExists("Assets/Data/Items/Crops");

        var crops = new (string name, string desc, int sell, int stamina)[]
        {
            ("Turnip",     "A humble root vegetable.",       35,  10),
            ("Potato",     "Hearty and filling.",            80,  15),
            ("Strawberry", "Sweet red berry from the farm.", 120, 25),
            ("Sunflower",  "A tall golden bloom that follows the sun.", 80, 20),
        };

        foreach (var (itemName, desc, sell, stamina) in crops)
        {
            var path = $"Assets/Data/Items/Crops/{itemName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(path) != null)
            {
                Debug.Log($"[ItemSOPopulator] Skipped existing: {path}");
                skipped++;
                continue;
            }

            var asset = ScriptableObject.CreateInstance<ItemDefinition>();
            asset.itemName = itemName;
            asset.description = desc;
            asset.sellPrice = sell;
            asset.buyPrice = 0; // crops are not purchasable from shop
            asset.category = ItemCategory.General;
            asset.staminaRestore = stamina;

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[ItemSOPopulator] Created: {path}");
            created++;
        }

        // ── Seeds ────────────────────────────────────────────────────────────
        EnsureFolderExists("Assets/Data/Items/Seeds");

        var seeds = new (string name, string desc, int buy)[]
        {
            ("Turnip Seed",     "Plant in spring. Grows quickly.",         20),
            ("Potato Seed",     "Plant in spring or summer.",              50),
            ("Strawberry Seed", "Plant in spring. Takes longer to grow.",  80),
            ("Sunflower Seed",  "Plant in summer. Brightens your farm.",   40),
        };

        foreach (var (itemName, desc, buy) in seeds)
        {
            var safeName = itemName.Replace(" ", "");
            var path = $"Assets/Data/Items/Seeds/{safeName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(path) != null)
            {
                Debug.Log($"[ItemSOPopulator] Skipped existing: {path}");
                skipped++;
                continue;
            }

            var asset = ScriptableObject.CreateInstance<ItemDefinition>();
            asset.itemName = itemName;
            asset.description = desc;
            asset.buyPrice = buy;
            asset.sellPrice = Mathf.RoundToInt(buy * 0.3f);
            asset.category = ItemCategory.Seed;
            asset.staminaRestore = 0;

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[ItemSOPopulator] Created: {path}");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Populate Starter Items",
            $"Done!\nCreated:  {created}\nSkipped (already existed):  {skipped}",
            "OK");
    }

    private static void EnsureFolderExists(string folderPath)
    {
        var parts = folderPath.Split('/');
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
