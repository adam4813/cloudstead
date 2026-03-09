#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility: Cloudstead ▸ Create Mayor Cutscenes
///
/// Creates Day1 and Day2 CutsceneDefinition assets in Assets/Data/Cutscenes/,
/// pre-populated with the mayor arc steps. Dialogue lines are created as
/// inline DialogueTree assets in Assets/Data/Cutscenes/MayorDialogue/.
///
/// After running:
///   1. Add CutsceneActor to Mayor GO (actorId = "Mayor")
///   2. Add CutsceneActor to Mayor's airship (actorId = "MayorAirship")
///   3. Add CutsceneActor to Player airship (actorId = "PlayerAirship")
///   4. Assign Day1/Day2 assets to MayorController fields
///   5. Fill in worldPosition values for MoveActor steps (set from scene)
/// </summary>
public static class CreateMayorCutscenes
{
    private const string CutsceneFolder  = "Assets/Data/Cutscenes";
    private const string DialogueFolder  = "Assets/Data/Cutscenes/MayorDialogue";

    [MenuItem("Cloudstead/Create Mayor Cutscenes")]
    public static void CreateAll()
    {
        EnsureFolder(CutsceneFolder);
        EnsureFolder(DialogueFolder);

        // ── Build dialogue trees ──────────────────────────────────────────

        var mayorDef    = FindOrWarnNPC("Mayor");

        var dlgArrival  = MakeDialogue("Mayor_Day1_Arrival",
            mayorDef, "Welcome to your new home in the clouds! Let me show you around.");

        var dlgField    = MakeDialogue("Mayor_Day1_Field",
            mayorDef, "Soil's soft here. Good for planting — here, I brought you some seeds to get started.");

        var dlgHouse    = MakeDialogue("Mayor_Day1_House",
            mayorDef, "Your home. Small, but it's yours. The bed inside is how you'll end your day.");

        var dlgBench    = MakeDialogue("Mayor_Day1_Bench",
            mayorDef, "There's a workbench here already. You'll figure it out.");

        var dlgDepart   = MakeDialogue("Mayor_Day1_Departure",
            mayorDef, "I'll swing back tomorrow morning. Get some rest — the clouds are quiet at night.");

        var dlgDay2Arr  = MakeDialogue("Mayor_Day2_Arrival",
            mayorDef, "Morning! Ready to see the town?");

        var dlgShop     = MakeDialogue("Mayor_Day2_Shop",
            mayorDef, "Mabel handles most of what you'll need. Seeds, supplies. Honest prices.");

        var dlgSquare   = MakeDialogue("Mayor_Day2_Square",
            mayorDef, "Town square. Folks gather here most mornings. Good place to catch up.");

        var dlgShipyard = MakeDialogue("Mayor_Day2_Shipyard",
            mayorDef, "Your airship was being finished up here. She's all yours now. Fly safe.");

        // ── Day 1 Cutscene ────────────────────────────────────────────────
        //
        // HUMAN TODO: Set worldPosition fields to your actual scene positions.
        // Steps marked "TODO" have placeholder (0,0) positions.

        var day1 = CreateOrReplace<CutsceneDefinition>("Mayor_Day1_Welcome");
        ApplySteps(day1, new[]
        {
            // Mayor appears near the player — camera focuses on him
            FocusCamera("Mayor"),
            Wait(0.5f),
            ShowDialogue(dlgArrival, mayorDef),

            // Walk to house
            Move("Mayor", Vector2.zero, "TODO: House entrance position"),
            Teleport("Player", new Vector2(1.5f, 0f), "TODO: offset from house position"),
            ShowDialogue(dlgHouse, mayorDef),

            // Walk to field area — player teleports alongside
            Move("Mayor", Vector2.zero, "TODO: Farm field position"),
            Teleport("Player", new Vector2(1.5f, 0f), "TODO: offset from field position"),
            ShowDialogue(dlgField, mayorDef),

            // Walk to crafting bench
            Move("Mayor", Vector2.zero, "TODO: Crafting bench position"),
            Teleport("Player", new Vector2(1.5f, 0f), "TODO: offset from bench position"),
            ShowDialogue(dlgBench, mayorDef),

            // Walk to airship dock
            Move("Mayor", Vector2.zero, "TODO: Dock position"),
            Teleport("Player", new Vector2(1.5f, 0f), "TODO: offset from dock position"),
            ShowDialogue(dlgDepart, mayorDef),

            // Return camera to player before mayor departs
            ReturnCamera(),
            SetActive("Mayor", false),
        });

        // ── Day 2 Cutscene ────────────────────────────────────────────────

        var day2 = CreateOrReplace<CutsceneDefinition>("Mayor_Day2_TownVisit");
        ApplySteps(day2, new[]
        {
            // Mayor appears outside the farmhouse door waiting for the player
            // TODO: Set worldPosition to just outside the farmhouse door
            Teleport("Mayor", Vector2.zero, "TODO: Outside farmhouse door"),
            SetActive("Mayor", true),
            FocusCamera("Mayor"),
            Wait(0.5f),
            ShowDialogue(dlgDay2Arr, mayorDef),

            // Player boards mayor's airship as passenger — camera follows airship
            FocusCamera("MayorAirship"),
            BoardAirship("Player", "MayorAirship"),

            // Mayor airship flies to town — TODO: set worldPosition to town cloud dock position
            Move("MayorAirship", Vector2.zero, "TODO: Town cloud dock position"),

            // Player disembarks at town — TODO: set town spawn pos + cloud id
            DisembarkAirship("Player", Vector2.zero, "town"),

            ReturnCamera(),
            Wait(0.3f),

            // Town tour — mayor walks, player teleports alongside
            Move("Mayor", Vector2.zero, "TODO: Mabel's shop position"),
            Teleport("Player", new Vector2(1.5f, 0f), "TODO: offset from shop position"),
            ShowDialogue(dlgShop, mayorDef),

            Move("Mayor", Vector2.zero, "TODO: Town square position"),
            Teleport("Player", new Vector2(1.5f, 0f), "TODO: offset from square position"),
            ShowDialogue(dlgSquare, mayorDef),

            // Shipyard handoff
            Move("Mayor", Vector2.zero, "TODO: Shipyard position"),
            Teleport("Player", new Vector2(1.5f, 0f), "TODO: offset from shipyard position"),
            ShowDialogue(dlgShipyard, mayorDef),

            // Transfer player airship ownership (ownerId 0 = player 0)
            TransferOwnership("PlayerAirship", 0),

            // Mayor departs
            ReturnCamera(),
            SetActive("Mayor", false),
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreateMayorCutscenes] Day 1 and Day 2 cutscenes created in Assets/Data/Cutscenes/.\n" +
                  "Open Cloudstead ▸ Cutscene Editor to set world positions on each MoveActor step.");

        Selection.activeObject = day1;
        EditorGUIUtility.PingObject(day1);
    }

    // ── Step factory helpers ─────────────────────────────────────────────

    private static CutsceneStep Wait(float sec)
        => new CutsceneStep { type = CutsceneStepType.Wait, duration = sec };

    private static CutsceneStep ShowDialogue(DialogueTree tree, NPCDefinition spk)
        => new CutsceneStep { type = CutsceneStepType.ShowDialogue, dialogue = tree, speaker = spk };

    private static CutsceneStep Teleport(string actor, Vector2 pos, string _ = null)
        => new CutsceneStep { type = CutsceneStepType.TeleportActor, actorId = actor, worldPosition = pos };

    private static CutsceneStep Move(string actor, Vector2 pos, string _ = null)
        => new CutsceneStep { type = CutsceneStepType.MoveActor, actorId = actor, worldPosition = pos };

    private static CutsceneStep SetActive(string actor, bool active)
        => new CutsceneStep { type = CutsceneStepType.SetActorActive, actorId = actor, boolValue = active };

    private static CutsceneStep FocusCamera(string targetActor)
        => new CutsceneStep { type = CutsceneStepType.FocusCamera, targetActorId = targetActor };

    private static CutsceneStep ReturnCamera()
        => new CutsceneStep { type = CutsceneStepType.ReturnCamera };

    private static CutsceneStep BoardAirship(string passenger, string airship)
        => new CutsceneStep { type = CutsceneStepType.BoardAirship, actorId = passenger, targetActorId = airship };

    private static CutsceneStep DisembarkAirship(string actor, Vector2 pos, string cloudId = null)
    {
        CloudIslandDefinition cloud = null;
        if (!string.IsNullOrEmpty(cloudId))
        {
            var guids = AssetDatabase.FindAssets("t:CloudIslandDefinition");
            foreach (var g in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<CloudIslandDefinition>(AssetDatabase.GUIDToAssetPath(g));
                if (def != null && def.cloudId == cloudId) { cloud = def; break; }
            }
        }
        return new CutsceneStep { type = CutsceneStepType.DisembarkAirship, actorId = actor, worldPosition = pos, disembarkCloud = cloud };
    }

    private static CutsceneStep TransferOwnership(string airshipActor, uint ownerId)
        => new CutsceneStep { type = CutsceneStepType.TransferOwnership, actorId = airshipActor, ownerIdValue = ownerId };

    // ── Asset helpers ────────────────────────────────────────────────────

    private static DialogueTree MakeDialogue(string assetName, NPCDefinition speaker, string line)
    {
        string path = $"{DialogueFolder}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueTree>(path);
        if (existing != null) return existing;

        var tree = ScriptableObject.CreateInstance<DialogueTree>();
        tree.nodes = new[]
        {
            new DialogueTree.DialogueNode
            {
                speakerName = speaker != null ? speaker.npcName : "Mayor",
                text = line,
                nextIndex = -1,
            }
        };
        AssetDatabase.CreateAsset(tree, path);
        return tree;
    }

    private static T CreateOrReplace<T>(string assetName) where T : ScriptableObject
    {
        string path = $"{CutsceneFolder}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            // Clear existing steps by replacing
            AssetDatabase.DeleteAsset(path);
        }
        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void ApplySteps(CutsceneDefinition def, CutsceneStep[] steps)
    {
        var so = new SerializedObject(def);

        // Set cutsceneId to asset name
        so.FindProperty("cutsceneId").stringValue = def.name;

        var prop = so.FindProperty("steps");
        prop.arraySize = steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            var s = steps[i];
            var e = prop.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("type").enumValueIndex          = (int)s.type;
            e.FindPropertyRelative("actorId").stringValue          = s.actorId ?? "";
            e.FindPropertyRelative("targetActorId").stringValue    = s.targetActorId ?? "";
            e.FindPropertyRelative("worldPosition").vector2Value   = s.worldPosition;
            e.FindPropertyRelative("duration").floatValue          = s.duration;
            e.FindPropertyRelative("boolValue").boolValue          = s.boolValue;
            e.FindPropertyRelative("ownerIdValue").longValue       = s.ownerIdValue;

            // Object references (dialogue tree, speaker, disembark cloud)
            var dialogueProp      = e.FindPropertyRelative("dialogue");
            var speakerProp       = e.FindPropertyRelative("speaker");
            var disembarkCloudProp = e.FindPropertyRelative("disembarkCloud");
            if (dialogueProp      != null) dialogueProp.objectReferenceValue      = s.dialogue;
            if (speakerProp       != null) speakerProp.objectReferenceValue       = s.speaker;
            if (disembarkCloudProp != null) disembarkCloudProp.objectReferenceValue = s.disembarkCloud;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(def);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static NPCDefinition FindOrWarnNPC(string npcName)
    {
        var guids = AssetDatabase.FindAssets("t:NPCDefinition");
        foreach (var g in guids)
        {
            var def = AssetDatabase.LoadAssetAtPath<NPCDefinition>(AssetDatabase.GUIDToAssetPath(g));
            if (def != null && def.npcName == npcName) return def;
        }
        Debug.LogWarning($"[CreateMayorCutscenes] NPCDefinition with npcName='{npcName}' not found. Speaker will be null.");
        return null;
    }
}
#endif
