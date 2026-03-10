using UnityEditor;
using UnityEngine;

public static class BuildingSOPopulator
{
    [MenuItem("Cloudstead/Populate Town Buildings")]
    private static void PopulateTownBuildings()
    {
        int createdBuildings = 0;
        int skippedBuildings = 0;
        int createdNPCs = 0;

        // ── Create NPC stubs for town NPCs that don't exist yet ──────────────

        EnsureFolderExists("Assets/Data/NPCs");

        var npcStubs = new (string fileName, string npcName, string[] greetings, string[] farewells,
            bool isMerchant, string defaultGift)[]
        {
            ("Forge", "Forge",
                new[]
                {
                    "Hmm? Oh, welcome. Mind the sparks.",
                    "The sky-iron's been good today.",
                    "Need something forged? Let me see what I can do."
                },
                new[] { "Keep your tools sharp.", "Safe skies.", "Come back when you need repairs." },
                true,
                "Ah, this'll make fine material. Much appreciated."),

            ("Barley", "Barley",
                new[]
                {
                    "Welcome to Cloud's Rest! Take a seat anywhere.",
                    "Ah, my favorite farmer! What'll it be?",
                    "The stew's on and the kettle's warm."
                },
                new[]
                {
                    "Don't be a stranger!",
                    "Clear skies and full bellies to you!",
                    "Come back any evening — there's always room."
                },
                false,
                "For me? You're too kind! I might just work this into tonight's recipe."),

            ("Riggins", "Riggins",
                new[]
                {
                    "Wind's fair today. Good flying weather.",
                    "Keep those engines tuned and she'll never let you down.",
                    "Ahoy! What brings you to the docks?"
                },
                new[] { "Fly safe out there.", "Keep her steady.", "Clear skies, captain." },
                true,
                "Useful. I can always find a place for good materials on the docks."),
        };

        foreach (var (fileName, npcName, greetings, farewells, isMerchant, defaultGift) in npcStubs)
        {
            var path = $"Assets/Data/NPCs/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<NPCDefinition>(path) != null)
                continue;

            var npc = ScriptableObject.CreateInstance<NPCDefinition>();
            npc.npcName = npcName;
            npc.greetings = greetings;
            npc.farewells = farewells;
            npc.isMerchant = isMerchant;
            npc.acceptsGifts = true;
            npc.defaultGiftResponse = defaultGift;

            AssetDatabase.CreateAsset(npc, path);
            Debug.Log($"[BuildingSOPopulator] Created NPC: {path}");
            createdNPCs++;
        }

        if (createdNPCs > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ── Load all NPC references ──────────────────────────────────────────

        var mayor   = AssetDatabase.LoadAssetAtPath<NPCDefinition>("Assets/Data/NPCs/Mayor.asset");
        var mabel   = AssetDatabase.LoadAssetAtPath<NPCDefinition>("Assets/Data/NPCs/Merchant_Mabel.asset");
        var forge   = AssetDatabase.LoadAssetAtPath<NPCDefinition>("Assets/Data/NPCs/Forge.asset");
        var barley  = AssetDatabase.LoadAssetAtPath<NPCDefinition>("Assets/Data/NPCs/Barley.asset");
        var riggins = AssetDatabase.LoadAssetAtPath<NPCDefinition>("Assets/Data/NPCs/Riggins.asset");

        WarnIfNull(mayor,   "Mayor");
        WarnIfNull(mabel,   "Merchant_Mabel");
        WarnIfNull(forge,   "Forge");
        WarnIfNull(barley,  "Barley");
        WarnIfNull(riggins, "Riggins");

        // ── Building definitions ─────────────────────────────────────────────

        EnsureFolderExists("Assets/Data/Buildings");

        var buildings = new (string name, BuildingType type, string desc,
            NPCDefinition owner, bool alwaysOpen, float open, float close)[]
        {
            // ── Service buildings ────────────────────────────────────────────
            ("Town Hall", BuildingType.Service,
                "The administrative heart of the village. Community notices and seasonal event postings line the walls. The Mayor holds office here.",
                mayor, true, 0, 0),

            ("The Mooring", BuildingType.Service,
                "Dockmaster Riggins keeps the sky-docks in order. Airship parts, hull repairs, and flight charts for the adventurous.",
                riggins, false, 7f, 17f),

            ("The Cloudmill", BuildingType.Service,
                "A gentle windmill whose sails turn lazily in the upper winds. Grain is milled and cloud-fiber spun into usable thread here.",
                null, false, 6f, 20f),

            ("Cloud Archives", BuildingType.Service,
                "Shelves of cloud-lore, pressed flowers, and research notes. Knowledge gathered from across the floating isles.",
                null, false, 8f, 20f),

            ("Cloud's Rest Tavern", BuildingType.Service,
                "A cozy gathering spot with warm drinks and warmer conversation. Barley's cooking draws folk from across the sky.",
                barley, false, 12f, 24f),

            // ── Shops ────────────────────────────────────────────────────────
            ("Mabel's Market", BuildingType.Shop,
                "A well-stocked general store near the cloud's edge. Seeds, tools, and sundries for the discerning sky farmer.",
                mabel, false, 8f, 18f),

            ("The Sky Forge", BuildingType.Shop,
                "A rooftop smithy where cloud-iron is hammered into sturdy tools and equipment. The ring of the anvil carries far on the wind.",
                forge, false, 10f, 18f),

            ("Apothecary", BuildingType.Shop,
                "Dried herbs hang from the ceiling like tiny chandeliers. Remedies and tonics brewed from sky-grown ingredients.",
                null, false, 9f, 17f),

            // ── Public / Decorative ──────────────────────────────────────────
            ("Sky Shrine", BuildingType.Public,
                "A quiet place of contemplation beneath an open sky. Seasonal ceremonies bring the whole village together.",
                null, true, 0, 0),

            ("The Wellspring", BuildingType.Decorative,
                "A gentle fountain at the village center where water rises from the cloud itself, sparkling in the light.",
                null, true, 0, 0),

            // ── Homes ────────────────────────────────────────────────────────
            ("Player's Farmhouse", BuildingType.Home,
                "Your cozy home among the clouds. A warm hearth, a soft bed, and endless sky out every window.",
                null, true, 0, 0),

            ("Mayor's Cottage", BuildingType.Home,
                "A tidy cottage with a wrap-around porch and climbing vines. The Mayor can always be found with tea in hand.",
                mayor, true, 0, 0),
        };

        foreach (var (bName, bType, desc, owner, alwaysOpen, open, close) in buildings)
        {
            var safeName = string.Concat(
                bName.Split(System.IO.Path.GetInvalidFileNameChars()))
                .Replace(" ", "").Replace("'", "");
            var path = $"Assets/Data/Buildings/{safeName}.asset";

            if (AssetDatabase.LoadAssetAtPath<BuildingDefinition>(path) != null)
            {
                Debug.Log($"[BuildingSOPopulator] Skipped existing: {path}");
                skippedBuildings++;
                continue;
            }

            var asset = ScriptableObject.CreateInstance<BuildingDefinition>();
            asset.buildingName = bName;
            asset.buildingType = bType;
            asset.description  = desc;
            asset.ownerNPC     = owner;
            asset.alwaysOpen   = alwaysOpen;
            asset.openHour     = open;
            asset.closeHour    = close;
            asset.daysOpen     = new bool[] { true, true, true, true, true, true, true };

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[BuildingSOPopulator] Created: {path}");
            createdBuildings++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var summary = $"Done!\n\n" +
                      $"Buildings created:  {createdBuildings}\n" +
                      $"Buildings skipped:  {skippedBuildings}\n" +
                      $"NPCs created:       {createdNPCs}";

        if (createdNPCs > 0)
            summary += "\n\nNew NPC stubs were created at Assets/Data/NPCs/.\n" +
                       "Assign portraits, liked gifts, and shop inventories.";

        summary += "\n\nOpen Cloudstead > Building Editor to browse and refine.";

        EditorUtility.DisplayDialog("Populate Town Buildings", summary, "OK");
    }

    private static void WarnIfNull(NPCDefinition npc, string name)
    {
        if (npc == null)
            Debug.LogWarning($"[BuildingSOPopulator] NPC '{name}' not found at Assets/Data/NPCs/{name}.asset — building will have null owner.");
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
