using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates placeholder pixel-art PNGs for town buildings (icons),
/// NPC overworld sprites, and NPC dialogue portraits.
/// Menu: Cloudstead > Generate Town Art
/// </summary>
public static class GenerateTownArt
{
    private const int ICON_SIZE = 32;
    private const int SPRITE_SIZE = 32;
    private const int PORTRAIT_SIZE = 128;

    [MenuItem("Cloudstead/Generate Town Art")]
    private static void Generate()
    {
        int created = 0;

        // ── Building Icons ───────────────────────────────────────────────────
        EnsureFolderExists("Assets/Art/Icons/Buildings");

        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_TownHall.png",
            DrawTownHall);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_MabelsMarket.png",
            DrawMarket);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_TheSkyForge.png",
            DrawSkyForge);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_CloudsRestTavern.png",
            DrawTavern);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_TheMooring.png",
            DrawMooring);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_TheCloudmill.png",
            DrawCloudmill);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_Apothecary.png",
            DrawApothecary);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_CloudArchives.png",
            DrawArchives);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_SkyShrine.png",
            DrawShrine);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_TheWellspring.png",
            DrawWellspring);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_PlayersFarmhouse.png",
            DrawFarmhouse);
        created += EmitIcon("Assets/Art/Icons/Buildings/Icon_MayorsCottage.png",
            DrawMayorsCottage);

        // ── NPC Sprites (32×32 top-down) ─────────────────────────────────────
        EnsureFolderExists("Assets/Art/Sprites/NPCs");

        created += EmitSprite("Assets/Art/Sprites/NPCs/NPC_Mayor.png",
            SPRITE_SIZE, SPRITE_SIZE, (tex) => DrawNPCSprite(tex,
                new Color32(60, 70, 120, 255),   // dark-blue coat
                new Color32(180, 180, 190, 255),  // silver hair
                new Color32(240, 210, 170, 255),  // skin
                true));                            // has hat

        created += EmitSprite("Assets/Art/Sprites/NPCs/NPC_Mabel.png",
            SPRITE_SIZE, SPRITE_SIZE, (tex) => DrawNPCSprite(tex,
                new Color32(80, 150, 80, 255),    // green apron
                new Color32(160, 100, 60, 255),   // brown hair
                new Color32(240, 210, 170, 255),  // skin
                false));

        created += EmitSprite("Assets/Art/Sprites/NPCs/NPC_Elm.png",
            SPRITE_SIZE, SPRITE_SIZE, (tex) => DrawNPCSprite(tex,
                new Color32(140, 120, 90, 255),   // earthy brown tunic
                new Color32(100, 70, 40, 255),    // dark-brown hair
                new Color32(220, 190, 150, 255),  // warm skin
                false));

        created += EmitSprite("Assets/Art/Sprites/NPCs/NPC_Forge.png",
            SPRITE_SIZE, SPRITE_SIZE, (tex) => DrawNPCSprite(tex,
                new Color32(80, 60, 50, 255),     // dark leather apron
                new Color32(40, 30, 30, 255),     // black hair
                new Color32(180, 140, 110, 255),  // tan skin
                false));

        created += EmitSprite("Assets/Art/Sprites/NPCs/NPC_Barley.png",
            SPRITE_SIZE, SPRITE_SIZE, (tex) => DrawNPCSprite(tex,
                new Color32(200, 180, 140, 255),  // cream/white chef outfit
                new Color32(220, 190, 130, 255),  // sandy-blonde hair
                new Color32(240, 210, 170, 255),  // skin
                true));                            // chef hat

        created += EmitSprite("Assets/Art/Sprites/NPCs/NPC_Riggins.png",
            SPRITE_SIZE, SPRITE_SIZE, (tex) => DrawNPCSprite(tex,
                new Color32(50, 80, 120, 255),    // navy captain coat
                new Color32(140, 130, 120, 255),  // salt-pepper hair
                new Color32(200, 170, 140, 255),  // weathered skin
                true));                            // captain's cap

        // ── NPC Portraits (128×128) ──────────────────────────────────────────
        EnsureFolderExists("Assets/Art/Sprites/Portraits");

        created += EmitSprite("Assets/Art/Sprites/Portraits/Portrait_Mayor.png",
            PORTRAIT_SIZE, PORTRAIT_SIZE, (tex) => DrawPortrait(tex,
                new Color32(60, 70, 120, 255),
                new Color32(180, 180, 190, 255),
                new Color32(240, 210, 170, 255),
                new Color32(100, 140, 200, 255),  // bg
                true));

        created += EmitSprite("Assets/Art/Sprites/Portraits/Portrait_Mabel.png",
            PORTRAIT_SIZE, PORTRAIT_SIZE, (tex) => DrawPortrait(tex,
                new Color32(80, 150, 80, 255),
                new Color32(160, 100, 60, 255),
                new Color32(240, 210, 170, 255),
                new Color32(120, 180, 120, 255),
                false));

        created += EmitSprite("Assets/Art/Sprites/Portraits/Portrait_Elm.png",
            PORTRAIT_SIZE, PORTRAIT_SIZE, (tex) => DrawPortrait(tex,
                new Color32(140, 120, 90, 255),
                new Color32(100, 70, 40, 255),
                new Color32(220, 190, 150, 255),
                new Color32(160, 150, 130, 255),
                false));

        created += EmitSprite("Assets/Art/Sprites/Portraits/Portrait_Forge.png",
            PORTRAIT_SIZE, PORTRAIT_SIZE, (tex) => DrawPortrait(tex,
                new Color32(80, 60, 50, 255),
                new Color32(40, 30, 30, 255),
                new Color32(180, 140, 110, 255),
                new Color32(140, 100, 80, 255),
                false));

        created += EmitSprite("Assets/Art/Sprites/Portraits/Portrait_Barley.png",
            PORTRAIT_SIZE, PORTRAIT_SIZE, (tex) => DrawPortrait(tex,
                new Color32(200, 180, 140, 255),
                new Color32(220, 190, 130, 255),
                new Color32(240, 210, 170, 255),
                new Color32(200, 190, 160, 255),
                true));

        created += EmitSprite("Assets/Art/Sprites/Portraits/Portrait_Riggins.png",
            PORTRAIT_SIZE, PORTRAIT_SIZE, (tex) => DrawPortrait(tex,
                new Color32(50, 80, 120, 255),
                new Color32(140, 130, 120, 255),
                new Color32(200, 170, 140, 255),
                new Color32(100, 130, 170, 255),
                true));

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Generate Town Art",
            $"Created {created} PNG files.\n\n" +
            "Building icons: Assets/Art/Icons/Buildings/\n" +
            "NPC sprites:    Assets/Art/Sprites/NPCs/\n" +
            "NPC portraits:  Assets/Art/Sprites/Portraits/\n\n" +
            "Wire icons to BuildingDefinition.icon and portraits to NPCDefinition.portrait.",
            "OK");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Building icon draw functions (32×32)
    // ═══════════════════════════════════════════════════════════════════════════

    private static void DrawTownHall(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255)); // sky bg
        // Building body
        FillRect(tex, 6, 4, 20, 16, new Color32(90, 110, 160, 255));
        // Roof
        FillTriangle(tex, 4, 20, 28, 20, 16, 28, new Color32(60, 70, 120, 255));
        // Door
        FillRect(tex, 13, 4, 6, 8, new Color32(140, 100, 60, 255));
        // Windows
        FillRect(tex, 8, 12, 4, 4, new Color32(255, 240, 180, 255));
        FillRect(tex, 20, 12, 4, 4, new Color32(255, 240, 180, 255));
        // Flag pole
        FillRect(tex, 26, 20, 1, 10, new Color32(120, 100, 80, 255));
        FillRect(tex, 27, 26, 4, 3, new Color32(200, 60, 60, 255));
    }

    private static void DrawMarket(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Building
        FillRect(tex, 4, 4, 24, 14, new Color32(180, 160, 120, 255));
        // Awning (green striped)
        FillRect(tex, 2, 18, 28, 4, new Color32(80, 150, 80, 255));
        FillRect(tex, 2, 20, 28, 1, new Color32(60, 120, 60, 255));
        // Door
        FillRect(tex, 13, 4, 6, 8, new Color32(100, 70, 40, 255));
        // Window
        FillRect(tex, 6, 10, 5, 4, new Color32(220, 240, 255, 255));
        FillRect(tex, 21, 10, 5, 4, new Color32(220, 240, 255, 255));
        // Sign
        FillRect(tex, 22, 22, 8, 5, new Color32(240, 220, 160, 255));
        FillRect(tex, 24, 23, 4, 3, new Color32(80, 150, 80, 255));
    }

    private static void DrawSkyForge(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Stone building
        FillRect(tex, 5, 4, 22, 16, new Color32(120, 110, 100, 255));
        // Roof
        FillRect(tex, 3, 20, 26, 3, new Color32(80, 70, 60, 255));
        // Chimney with glow
        FillRect(tex, 22, 20, 4, 8, new Color32(100, 90, 80, 255));
        FillRect(tex, 23, 28, 2, 2, new Color32(255, 160, 60, 255));
        // Forge glow (door/opening)
        FillRect(tex, 11, 4, 8, 8, new Color32(255, 140, 40, 255));
        FillRect(tex, 13, 4, 4, 6, new Color32(255, 200, 80, 255));
        // Anvil shape
        FillRect(tex, 6, 6, 4, 2, new Color32(60, 60, 70, 255));
        FillRect(tex, 7, 4, 2, 2, new Color32(60, 60, 70, 255));
    }

    private static void DrawTavern(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Warm wooden building
        FillRect(tex, 4, 4, 24, 16, new Color32(160, 120, 70, 255));
        // Roof
        FillTriangle(tex, 2, 20, 30, 20, 16, 28, new Color32(130, 80, 40, 255));
        // Door
        FillRect(tex, 13, 4, 6, 8, new Color32(100, 60, 30, 255));
        // Warm window glow
        FillRect(tex, 6, 10, 5, 5, new Color32(255, 220, 140, 255));
        FillRect(tex, 21, 10, 5, 5, new Color32(255, 220, 140, 255));
        // Mug sign
        FillRect(tex, 14, 14, 4, 3, new Color32(200, 180, 140, 255));
        FillRect(tex, 18, 15, 1, 2, new Color32(200, 180, 140, 255));
    }

    private static void DrawMooring(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Dock platform
        FillRect(tex, 2, 4, 28, 6, new Color32(140, 120, 80, 255));
        FillRect(tex, 4, 5, 24, 1, new Color32(120, 100, 60, 255));
        // Mooring posts
        FillRect(tex, 4, 10, 3, 10, new Color32(100, 80, 50, 255));
        FillRect(tex, 25, 10, 3, 10, new Color32(100, 80, 50, 255));
        // Ropes
        FillRect(tex, 7, 16, 18, 1, new Color32(180, 160, 120, 255));
        // Small hut
        FillRect(tex, 10, 10, 12, 8, new Color32(80, 110, 140, 255));
        FillRect(tex, 8, 18, 16, 2, new Color32(60, 80, 110, 255));
        // Lantern
        FillRect(tex, 5, 20, 1, 2, new Color32(255, 220, 100, 255));
    }

    private static void DrawCloudmill(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Tower
        FillRect(tex, 11, 4, 10, 18, new Color32(200, 190, 170, 255));
        // Cap
        FillTriangle(tex, 10, 22, 22, 22, 16, 28, new Color32(160, 140, 110, 255));
        // Sails (X shape)
        FillRect(tex, 15, 14, 2, 14, new Color32(220, 220, 230, 255)); // vertical
        FillRect(tex, 4, 20, 24, 2, new Color32(220, 220, 230, 255));  // horizontal
        // Hub
        FillRect(tex, 14, 20, 4, 4, new Color32(140, 120, 90, 255));
        // Door
        FillRect(tex, 14, 4, 4, 5, new Color32(120, 90, 60, 255));
        // Cloud wisps at base
        FillRect(tex, 2, 2, 8, 3, new Color32(230, 240, 250, 200));
        FillRect(tex, 22, 3, 8, 2, new Color32(230, 240, 250, 200));
    }

    private static void DrawApothecary(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Building (purple-green)
        FillRect(tex, 5, 4, 22, 16, new Color32(120, 100, 140, 255));
        // Roof
        FillRect(tex, 3, 20, 26, 3, new Color32(90, 70, 110, 255));
        // Door
        FillRect(tex, 13, 4, 6, 8, new Color32(80, 60, 100, 255));
        // Green cross / herb symbol
        FillRect(tex, 14, 14, 4, 2, new Color32(80, 180, 80, 255));
        FillRect(tex, 15, 13, 2, 4, new Color32(80, 180, 80, 255));
        // Bottle in window
        FillRect(tex, 7, 10, 3, 5, new Color32(160, 220, 180, 255));
        FillRect(tex, 8, 15, 1, 1, new Color32(100, 160, 120, 255));
        // Hanging herbs
        FillRect(tex, 22, 17, 1, 3, new Color32(60, 140, 60, 255));
        FillRect(tex, 24, 18, 1, 2, new Color32(80, 160, 60, 255));
    }

    private static void DrawArchives(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Tall stone building
        FillRect(tex, 6, 4, 20, 20, new Color32(140, 130, 120, 255));
        // Peaked roof
        FillTriangle(tex, 4, 24, 28, 24, 16, 30, new Color32(100, 90, 80, 255));
        // Arched door
        FillRect(tex, 13, 4, 6, 7, new Color32(80, 60, 40, 255));
        FillRect(tex, 14, 11, 4, 1, new Color32(80, 60, 40, 255));
        // Book symbol
        FillRect(tex, 14, 16, 5, 4, new Color32(180, 150, 100, 255));
        FillRect(tex, 16, 16, 1, 4, new Color32(140, 110, 70, 255));
        // Window
        FillRect(tex, 8, 14, 3, 6, new Color32(200, 220, 240, 255));
        FillRect(tex, 21, 14, 3, 6, new Color32(200, 220, 240, 255));
    }

    private static void DrawShrine(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Base platform
        FillRect(tex, 6, 4, 20, 4, new Color32(200, 200, 210, 255));
        // Pillars
        FillRect(tex, 8, 8, 3, 14, new Color32(200, 200, 210, 255));
        FillRect(tex, 21, 8, 3, 14, new Color32(200, 200, 210, 255));
        // Arch top
        FillRect(tex, 8, 22, 16, 2, new Color32(200, 200, 210, 255));
        // Star/glow in center
        FillRect(tex, 14, 14, 4, 4, new Color32(255, 240, 140, 255));
        FillRect(tex, 15, 13, 2, 6, new Color32(255, 220, 100, 255));
        FillRect(tex, 13, 15, 6, 2, new Color32(255, 220, 100, 255));
        // Cloud wisps
        FillRect(tex, 1, 2, 6, 2, new Color32(230, 240, 250, 200));
        FillRect(tex, 25, 3, 6, 2, new Color32(230, 240, 250, 200));
    }

    private static void DrawWellspring(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Circular basin (approximated)
        FillRect(tex, 8, 6, 16, 12, new Color32(160, 170, 190, 255));
        FillRect(tex, 6, 8, 20, 8, new Color32(160, 170, 190, 255));
        // Water
        FillRect(tex, 10, 8, 12, 8, new Color32(100, 180, 240, 255));
        FillRect(tex, 8, 10, 16, 4, new Color32(100, 180, 240, 255));
        // Sparkle
        FillRect(tex, 15, 13, 2, 2, new Color32(200, 230, 255, 255));
        FillRect(tex, 11, 11, 1, 1, new Color32(220, 240, 255, 255));
        // Water spout center
        FillRect(tex, 14, 14, 4, 8, new Color32(140, 150, 170, 255));
        FillRect(tex, 15, 22, 2, 3, new Color32(140, 200, 240, 200));
    }

    private static void DrawFarmhouse(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Wooden house
        FillRect(tex, 5, 4, 22, 14, new Color32(170, 140, 90, 255));
        // Roof
        FillTriangle(tex, 3, 18, 29, 18, 16, 26, new Color32(140, 80, 50, 255));
        // Door
        FillRect(tex, 13, 4, 6, 8, new Color32(110, 70, 40, 255));
        // Window with warm glow
        FillRect(tex, 6, 10, 5, 4, new Color32(255, 230, 160, 255));
        FillRect(tex, 21, 10, 5, 4, new Color32(255, 230, 160, 255));
        // Chimney
        FillRect(tex, 22, 22, 3, 6, new Color32(130, 110, 90, 255));
        // Smoke puff
        FillRect(tex, 23, 28, 1, 2, new Color32(220, 220, 230, 180));
        // Flower box
        FillRect(tex, 6, 9, 5, 1, new Color32(100, 160, 80, 255));
    }

    private static void DrawMayorsCottage(Texture2D tex)
    {
        Clear(tex, new Color32(180, 210, 240, 255));
        // Light-blue cottage
        FillRect(tex, 5, 4, 22, 14, new Color32(150, 180, 210, 255));
        // Roof
        FillTriangle(tex, 3, 18, 29, 18, 16, 26, new Color32(110, 130, 160, 255));
        // Porch wrap (bottom)
        FillRect(tex, 3, 4, 26, 3, new Color32(180, 160, 120, 255));
        // Door
        FillRect(tex, 13, 4, 6, 8, new Color32(100, 80, 60, 255));
        // Windows
        FillRect(tex, 6, 10, 5, 5, new Color32(230, 240, 255, 255));
        FillRect(tex, 21, 10, 5, 5, new Color32(230, 240, 255, 255));
        // Climbing vines
        FillRect(tex, 4, 8, 1, 10, new Color32(60, 140, 60, 255));
        FillRect(tex, 5, 12, 1, 4, new Color32(80, 160, 80, 255));
        FillRect(tex, 27, 10, 1, 8, new Color32(60, 140, 60, 255));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  NPC Sprite draw (32×32 top-down)
    // ═══════════════════════════════════════════════════════════════════════════

    private static void DrawNPCSprite(Texture2D tex, Color32 outfit, Color32 hair,
        Color32 skin, bool hasHat)
    {
        Clear(tex, Color.clear);
        int cx = 16;

        // Shadow
        FillEllipse(tex, cx - 5, 1, 10, 4, new Color32(0, 0, 0, 40));

        // Body / outfit
        FillRect(tex, cx - 5, 4, 10, 12, outfit);
        // Darken sides for depth
        FillRect(tex, cx - 5, 4, 1, 12, Darken(outfit, 0.8f));
        FillRect(tex, cx + 4, 4, 1, 12, Darken(outfit, 0.8f));

        // Arms
        FillRect(tex, cx - 7, 8, 2, 6, Darken(outfit, 0.9f));
        FillRect(tex, cx + 5, 8, 2, 6, Darken(outfit, 0.9f));
        // Hands
        FillRect(tex, cx - 7, 6, 2, 2, skin);
        FillRect(tex, cx + 5, 6, 2, 2, skin);

        // Head (skin)
        FillRect(tex, cx - 4, 16, 8, 8, skin);
        // Hair
        FillRect(tex, cx - 4, 22, 8, 3, hair);
        FillRect(tex, cx - 5, 20, 1, 4, hair);
        FillRect(tex, cx + 4, 20, 1, 4, hair);

        // Eyes
        tex.SetPixel(cx - 2, 19, new Color32(40, 40, 50, 255));
        tex.SetPixel(cx + 1, 19, new Color32(40, 40, 50, 255));

        // Smile
        tex.SetPixel(cx - 1, 17, new Color32(180, 100, 80, 255));
        tex.SetPixel(cx, 17, new Color32(180, 100, 80, 255));

        if (hasHat)
        {
            FillRect(tex, cx - 5, 24, 10, 3, Darken(outfit, 0.7f));
            FillRect(tex, cx - 3, 27, 6, 2, Darken(outfit, 0.7f));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  NPC Portrait draw (128×128)
    // ═══════════════════════════════════════════════════════════════════════════

    private static void DrawPortrait(Texture2D tex, Color32 outfit, Color32 hair,
        Color32 skin, Color32 bg, bool hasHat)
    {
        int w = tex.width, h = tex.height;
        Clear(tex, bg);

        // Soft vignette border
        int b = 4;
        FillRect(tex, 0, 0, w, b, Darken(bg, 0.85f));
        FillRect(tex, 0, h - b, w, b, Darken(bg, 0.85f));
        FillRect(tex, 0, 0, b, h, Darken(bg, 0.85f));
        FillRect(tex, w - b, 0, b, h, Darken(bg, 0.85f));

        int cx = w / 2;

        // Shoulders / outfit
        FillEllipse(tex, cx - 30, 4, 60, 30, outfit);
        FillRect(tex, cx - 30, 4, 60, 16, outfit);
        // Collar detail
        FillRect(tex, cx - 4, 28, 8, 6, Lighten(outfit, 1.15f));

        // Neck
        FillRect(tex, cx - 8, 30, 16, 10, skin);

        // Head
        FillEllipse(tex, cx - 20, 36, 40, 48, skin);

        // Hair (top & sides)
        FillEllipse(tex, cx - 22, 68, 44, 22, hair);
        FillRect(tex, cx - 22, 50, 4, 30, hair);
        FillRect(tex, cx + 18, 50, 4, 30, hair);

        // Eyes
        FillRect(tex, cx - 10, 56, 6, 6, Color.white);
        FillRect(tex, cx + 4, 56, 6, 6, Color.white);
        FillRect(tex, cx - 8, 57, 3, 4, new Color32(60, 80, 120, 255));
        FillRect(tex, cx + 6, 57, 3, 4, new Color32(60, 80, 120, 255));
        // Pupil
        FillRect(tex, cx - 7, 58, 2, 2, new Color32(30, 30, 40, 255));
        FillRect(tex, cx + 7, 58, 2, 2, new Color32(30, 30, 40, 255));
        // Eye shine
        tex.SetPixel(cx - 6, 59, new Color32(255, 255, 255, 200));
        tex.SetPixel(cx + 8, 59, new Color32(255, 255, 255, 200));

        // Eyebrows
        FillRect(tex, cx - 11, 62, 7, 2, Darken(hair, 0.7f));
        FillRect(tex, cx + 4, 62, 7, 2, Darken(hair, 0.7f));

        // Nose
        FillRect(tex, cx - 2, 50, 4, 4, Darken(skin, 0.92f));

        // Mouth (warm smile)
        FillRect(tex, cx - 5, 44, 10, 3, new Color32(200, 120, 100, 255));
        FillRect(tex, cx - 3, 46, 6, 1, Darken(skin, 0.95f));

        // Cheek blush (cozy touch)
        FillEllipse(tex, cx - 16, 48, 8, 4, new Color32(240, 160, 140, 100));
        FillEllipse(tex, cx + 8, 48, 8, 4, new Color32(240, 160, 140, 100));

        if (hasHat)
        {
            var hatColor = Darken(outfit, 0.7f);
            FillRect(tex, cx - 26, 80, 52, 6, hatColor);
            FillEllipse(tex, cx - 18, 84, 36, 16, hatColor);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Drawing primitives
    // ═══════════════════════════════════════════════════════════════════════════

    private static void Clear(Texture2D tex, Color col)
    {
        var pixels = new Color[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels(pixels);
    }

    private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color col)
    {
        for (int py = y; py < y + h && py < tex.height; py++)
            for (int px = x; px < x + w && px < tex.width; px++)
                if (px >= 0 && py >= 0)
                    tex.SetPixel(px, py, AlphaBlend(tex.GetPixel(px, py), col));
    }

    private static void FillTriangle(Texture2D tex, int x0, int y0, int x1, int y1, int x2, int y2, Color col)
    {
        int minX = Mathf.Max(0, Mathf.Min(x0, Mathf.Min(x1, x2)));
        int maxX = Mathf.Min(tex.width - 1, Mathf.Max(x0, Mathf.Max(x1, x2)));
        int minY = Mathf.Max(0, Mathf.Min(y0, Mathf.Min(y1, y2)));
        int maxY = Mathf.Min(tex.height - 1, Mathf.Max(y0, Mathf.Max(y1, y2)));

        for (int py = minY; py <= maxY; py++)
        {
            for (int px = minX; px <= maxX; px++)
            {
                if (PointInTriangle(px, py, x0, y0, x1, y1, x2, y2))
                    tex.SetPixel(px, py, AlphaBlend(tex.GetPixel(px, py), col));
            }
        }
    }

    private static void FillEllipse(Texture2D tex, int x, int y, int w, int h, Color col)
    {
        float cx = x + w * 0.5f;
        float cy = y + h * 0.5f;
        float rx = w * 0.5f;
        float ry = h * 0.5f;

        for (int py = y; py < y + h && py < tex.height; py++)
        {
            for (int px = x; px < x + w && px < tex.width; px++)
            {
                if (px < 0 || py < 0) continue;
                float dx = (px + 0.5f - cx) / rx;
                float dy = (py + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                    tex.SetPixel(px, py, AlphaBlend(tex.GetPixel(px, py), col));
            }
        }
    }

    private static bool PointInTriangle(int px, int py, int x0, int y0, int x1, int y1, int x2, int y2)
    {
        float d1 = Sign(px, py, x0, y0, x1, y1);
        float d2 = Sign(px, py, x1, y1, x2, y2);
        float d3 = Sign(px, py, x2, y2, x0, y0);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(int px, int py, int x0, int y0, int x1, int y1) =>
        (px - x1) * (y0 - y1) - (x0 - x1) * (py - y1);

    private static Color AlphaBlend(Color dst, Color src)
    {
        float a = src.a;
        if (a >= 1f) return src;
        if (a <= 0f) return dst;
        return new Color(
            dst.r * (1 - a) + src.r * a,
            dst.g * (1 - a) + src.g * a,
            dst.b * (1 - a) + src.b * a,
            Mathf.Max(dst.a, a));
    }

    private static Color32 Darken(Color32 c, float factor) =>
        new Color32(
            (byte)Mathf.Clamp(c.r * factor, 0, 255),
            (byte)Mathf.Clamp(c.g * factor, 0, 255),
            (byte)Mathf.Clamp(c.b * factor, 0, 255), c.a);

    private static Color32 Lighten(Color32 c, float factor) =>
        new Color32(
            (byte)Mathf.Clamp(c.r * factor, 0, 255),
            (byte)Mathf.Clamp(c.g * factor, 0, 255),
            (byte)Mathf.Clamp(c.b * factor, 0, 255), c.a);

    // ═══════════════════════════════════════════════════════════════════════════
    //  File output
    // ═══════════════════════════════════════════════════════════════════════════

    private delegate void DrawFunc(Texture2D tex);

    private static int EmitIcon(string assetPath, DrawFunc draw)
    {
        if (File.Exists(assetPath)) { Debug.Log($"[GenerateTownArt] Skipped existing: {assetPath}"); return 0; }
        var tex = new Texture2D(ICON_SIZE, ICON_SIZE, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        draw(tex);
        tex.Apply();
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"[GenerateTownArt] Created: {assetPath}");
        return 1;
    }

    private static int EmitSprite(string assetPath, int w, int h, DrawFunc draw)
    {
        if (File.Exists(assetPath)) { Debug.Log($"[GenerateTownArt] Skipped existing: {assetPath}"); return 0; }
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        draw(tex);
        tex.Apply();
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"[GenerateTownArt] Created: {assetPath}");
        return 1;
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
