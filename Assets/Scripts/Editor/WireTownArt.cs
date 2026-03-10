using UnityEditor;
using UnityEngine;

/// <summary>
/// After running Generate Town Art, this wires the generated PNGs
/// to their matching BuildingDefinition and NPCDefinition assets.
/// Menu: Cloudstead > Wire Town Art to Data
/// </summary>
public static class WireTownArt
{
    [MenuItem("Cloudstead/Wire Town Art to Data")]
    private static void Wire()
    {
        int wired = 0;

        // ── Building icons ───────────────────────────────────────────────────

        var iconMap = new (string buildingAsset, string iconPng)[]
        {
            ("TownHall",           "Icon_TownHall"),
            ("MabelsMarket",       "Icon_MabelsMarket"),
            ("TheSkyForge",        "Icon_TheSkyForge"),
            ("CloudsRestTavern",   "Icon_CloudsRestTavern"),
            ("TheMooring",         "Icon_TheMooring"),
            ("TheCloudmill",       "Icon_TheCloudmill"),
            ("Apothecary",         "Icon_Apothecary"),
            ("CloudArchives",      "Icon_CloudArchives"),
            ("SkyShrine",          "Icon_SkyShrine"),
            ("TheWellspring",      "Icon_TheWellspring"),
            ("PlayersFarmhouse",   "Icon_PlayersFarmhouse"),
            ("MayorsCottage",      "Icon_MayorsCottage"),
        };

        foreach (var (buildingFile, iconFile) in iconMap)
        {
            var building = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(
                $"Assets/Data/Buildings/{buildingFile}.asset");
            if (building == null) continue;

            var sprite = LoadSpriteAt($"Assets/Art/Icons/Buildings/{iconFile}.png");
            if (sprite == null) continue;
            if (building.icon == sprite) continue;

            building.icon = sprite;
            EditorUtility.SetDirty(building);
            Debug.Log($"[WireTownArt] {buildingFile}.icon → {iconFile}");
            wired++;
        }

        // ── NPC portraits ────────────────────────────────────────────────────

        var portraitMap = new (string npcAsset, string portraitPng)[]
        {
            ("Mayor",           "Portrait_Mayor"),
            ("Merchant_Mabel",  "Portrait_Mabel"),
            ("Villager_Elm",    "Portrait_Elm"),
            ("Forge",           "Portrait_Forge"),
            ("Barley",          "Portrait_Barley"),
            ("Riggins",         "Portrait_Riggins"),
        };

        foreach (var (npcFile, portraitFile) in portraitMap)
        {
            var npc = AssetDatabase.LoadAssetAtPath<NPCDefinition>(
                $"Assets/Data/NPCs/{npcFile}.asset");
            if (npc == null) continue;

            var sprite = LoadSpriteAt($"Assets/Art/Sprites/Portraits/{portraitFile}.png");
            if (sprite == null) continue;
            if (npc.portrait == sprite) continue;

            npc.portrait = sprite;
            EditorUtility.SetDirty(npc);
            Debug.Log($"[WireTownArt] {npcFile}.portrait → {portraitFile}");
            wired++;
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Wire Town Art",
            $"Wired {wired} sprite references.\n\n" +
            "NPC overworld sprites (Assets/Art/Sprites/NPCs/) must be\n" +
            "assigned manually to NPCController prefabs in the scene.",
            "OK");
    }

    private static Sprite LoadSpriteAt(string path)
    {
        // After import, Unity stores sprites as sub-assets of the texture
        var objs = AssetDatabase.LoadAllAssetsAtPath(path);
        if (objs == null) return null;
        foreach (var obj in objs)
        {
            if (obj is Sprite s) return s;
        }
        // If no sprite sub-asset yet, the texture import settings may not be Sprite type.
        // Force reimport as Sprite.
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            objs = AssetDatabase.LoadAllAssetsAtPath(path);
            if (objs != null)
                foreach (var obj in objs)
                    if (obj is Sprite s) return s;
        }
        return null;
    }
}
