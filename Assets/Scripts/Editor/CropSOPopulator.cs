using UnityEditor;
using UnityEngine;

public static class CropSOPopulator
{
    [MenuItem("Cloudstead/Populate Starter Crops")]
    private static void PopulateStarterCrops()
    {
        int created = 0;
        int skipped = 0;

        EnsureFolderExists("Assets/Data/Crops");

        var cropData = new (
            string cropName,
            string desc,
            int    growthDays,
            Season[] growSeasons,
            string seedAssetName,       // name in Assets/Data/Items/Seeds/
            string harvestItemName,     // name in Assets/Data/Items/Crops/
            int    minYield,
            int    maxYield,
            int    regrowDays
        )[]
        {
            (
                "Turnip",
                "A humble root vegetable that grows quickly in spring.",
                4,
                new[]{ Season.Spring },
                "TurnipSeed",
                "Turnip",
                1, 2, -1
            ),
            (
                "Potato",
                "Hearty and filling — grows through spring and summer.",
                6,
                new[]{ Season.Spring, Season.Summer },
                "PotatoSeed",
                "Potato",
                1, 3, -1
            ),
            (
                "Strawberry",
                "Sweet red berries that keep producing all spring.",
                8,
                new[]{ Season.Spring },
                "StrawberrySeed",
                "Strawberry",
                1, 3, 4
            ),
            (
                "Sunflower",
                "A tall golden bloom that follows the sun across summer skies.",
                7,
                new[]{ Season.Summer },
                "SunflowerSeed",
                "Sunflower",
                1, 2, -1
            ),
        };

        foreach (var c in cropData)
        {
            var safeName = c.cropName.Replace(" ", "");
            var path     = $"Assets/Data/Crops/{safeName}.asset";

            if (AssetDatabase.LoadAssetAtPath<CropDefinition>(path) != null)
            {
                Debug.Log($"[CropSOPopulator] Skipped existing: {path}");
                skipped++;
                continue;
            }

            var seedItem    = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/Data/Items/Seeds/{c.seedAssetName}.asset");
            var harvestItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/Data/Items/Crops/{c.harvestItemName}.asset");

            if (seedItem == null)
                Debug.LogWarning($"[CropSOPopulator] Seed item not found for '{c.cropName}': Assets/Data/Items/Seeds/{c.seedAssetName}.asset — run Populate Starter Items first.");
            if (harvestItem == null)
                Debug.LogWarning($"[CropSOPopulator] Harvest item not found for '{c.cropName}': Assets/Data/Items/Crops/{c.harvestItemName}.asset — run Populate Starter Items first.");

            var asset           = ScriptableObject.CreateInstance<CropDefinition>();
            asset.cropName      = c.cropName;
            asset.description   = c.desc;
            asset.seedItem      = seedItem;   // may be null — user can assign later
            asset.growthDays    = c.growthDays;
            asset.growSeasons   = c.growSeasons;
            asset.regrowDays    = c.regrowDays;
            asset.stageSprites  = new UnityEngine.Sprite[4]; // user assigns sprites in editor

            asset.harvestOutputs = harvestItem != null
                ? new HarvestOutput[]
                  {
                      new HarvestOutput { item = harvestItem, minYield = c.minYield, maxYield = c.maxYield }
                  }
                : new HarvestOutput[0];

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[CropSOPopulator] Created: {path}");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Populate Starter Crops",
            $"Done!\nCreated: {created}\nSkipped: {skipped}\n\nAssign stage sprites in the Crop Editor or directly on each CropDefinition SO.",
            "OK");
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
