# Cloudstead — Gym Testing Setup Guide

This document walks you through setting up each gym scene so you can play-test the code that's been written. Complete each section in order — each gym builds on the previous one.

---

## Prerequisites

1. **Unity 6+ open** with the Cloudstead project
2. **Refresh Assets**: Right-click `Assets` → Reimport All (ensures new sprites/sounds/scripts are imported)
3. **Sprite Import Settings**: Select ALL PNGs in `Assets/Art/Sprites/`, `Assets/Art/Tiles/`, and `Assets/Art/UI/`:
   - Texture Type: **Sprite (2D and UI)**
   - Pixels Per Unit: **32**
   - Filter Mode: **Point (no filter)**
   - Compression: **None**
   - Click **Apply**
4. **Create Tile Assets**: After importing sprites, use the menu: **Cloudstead → Create Tile Assets From Sprites**
   - This runs the `TileCreator` editor script which auto-creates `.asset` Tile files from all tile PNGs
   - Creates: `CloudGround.asset`, `CloudEdge.asset`, `SkyEmpty.asset`, `TilledSoil.asset`, `WateredSoil.asset`
   - All in `Assets/Art/Tiles/`
5. **Create Sorting Layers** (Edit → Project Settings → Tags and Layers → Sorting Layers):
   - `Background` (order 0)
   - `Ground` (order 1)
   - `GroundDecor` (order 2)
   - `Objects` (order 3)
   - `Characters` (order 4)
   - `UI` (order 5)
6. **Create Layers** (Tags and Layers → Layers):
   - Layer 6: `Ground`
   - Layer 7: `Interactable`
   - Layer 8: `Player`
7. **Create Tags**: `Player`

---

## 🧪 Gym 1: Player & Cloud

### Scene Setup

1. **Create Farm scene** (`Assets/Scenes/Farm.unity`):
   - Delete the default `Main Camera` and `Directional Light`

2. **Create Grid hierarchy**:
   - `GameObject → 2D Object → Tilemap → Rectangular`
   - Rename the Grid to `Grid`
   - Rename the child Tilemap to `GroundTilemap`
     - TilemapRenderer: Sorting Layer = `Ground`, Order = 0
     - Add `TilemapCollider2D` component
     - Add `Rigidbody2D` (Body Type: **Static**)
     - Add `CompositeCollider2D`
     - On the `TilemapCollider2D`: set Composite Operation = **Merge**
   - Create second child Tilemap: `SoilTilemap`
     - TilemapRenderer: Sorting Layer = `Ground`, Order = 1
   - Create third child Tilemap: `DecorationTilemap`
     - TilemapRenderer: Sorting Layer = `GroundDecor`, Order = 0

3. **Create CloudGeneration object** (empty GameObject):
   - Attach `CloudGenerator`:
     - cloudWidth: 30, cloudHeight: 30
     - noiseScale: 0.15, threshold: 0.4
     - Drag `GroundTilemap` into groundTilemap field
     - Drag `Assets/Art/Tiles/CloudGround.asset` into the **cloudTile** field
     - Drag `Assets/Art/Tiles/CloudEdge.asset` into the **edgeTile** field
     - (These .asset files were created by **Cloudstead → Create Tile Assets From Sprites** in Prerequisites step 4)
   - Attach `CloudBoundary`:
     - Drag `CloudGenerator` component reference into cloudGenerator field
     - Add `CompositeCollider2D` and `Rigidbody2D` (Static) to this object

4. **Create TileManager object** (empty GameObject):
   - Attach `TileManager`
   - Drag all three tilemaps into the fields (groundTilemap, soilTilemap, decorationTilemap)

5. **Create Player prefab** (`Assets/Prefabs/Player/Player.prefab`):
   - Create empty GameObject named "Player"
   - **Tag**: Player, **Layer**: Player
   - Add `SpriteRenderer`: sprite = `Player.png`, Sorting Layer = `Characters`, Order = 0
   - Add `Rigidbody2D`: Gravity Scale = 0, Freeze Rotation Z = ✓, Collision Detection = Continuous
   - Add `CapsuleCollider2D`: Size = (0.8, 0.5), Offset = (0, -0.25)
   - Add `PlayerController`: moveSpeed = 4
   - Add `PlayerInteraction`: interactionRadius = 1.5, interactableMask = `Interactable` layer
   - Add `PlayerInput` component:
     - Actions: drag the `InputSystem_Actions` asset (found at `Assets/InputSystem_Actions.inputactions`)
     - Default Map: **Player**
     - Behavior: **Invoke Unity Events**
     - **Wiring the events** (this is the critical part):
       1. In the PlayerInput Inspector, expand **Events → Player**
       2. You'll see entries for each action: Move, Interact, UseTool, OpenInventory, Pause, QuickbarSlot1-5
       3. For **Move**: click `+`, drag the **Player GameObject** into the object field, then select `PlayerController → OnMove` from the dropdown
       4. For **Interact**: click `+`, drag Player, select `PlayerInteraction → OnInteract`
       5. For **UseTool**: click `+`, drag Player, select `ToolController → OnUseTool`
       6. For **QuickbarSlot1**: click `+`, drag Player, select `ToolController → OnQuickbarSlot1`
       7. Repeat for QuickbarSlot2-5 → `ToolController → OnQuickbarSlot2` through `OnQuickbarSlot5`
       8. Leave OpenInventory and Pause unwired for now (will wire in Gym 3)
   - Place in scene at position (15, 15, 0)
   - Save as prefab in `Assets/Prefabs/Player/`

6. **Create Camera**:
   - Create Camera (Tag: MainCamera)
   - Set Projection: Orthographic, Size: **8**
   - Attach `CameraController`:
     - target: drag Player transform
     - smoothSpeed: 5
   - Add `AudioListener` (should be there by default)
   - **If URP**: ensure camera has Universal Additional Camera Data component

7. **Create Managers** (empty GameObjects in scene root):
   - "GameBootstrapper" → attach `GameBootstrapper`
   - "GameManager" → attach `GameManager`

### Play Test — Player & Cloud

Enter **Play mode** and evaluate:
- [x] Player spawns on a procedurally generated cloud shape
- [x] 8-directional movement feels responsive (WASD or arrow keys)
- [x] Movement speed is comfortable (not too fast, not sluggish)
- [x] Cloud shape looks organic and interesting (not a perfect rectangle)
- [x] Walking to cloud edges: player is gently pushed back (not a hard wall)
- [x] Camera follows smoothly without jitter
- [x] Cloud feels like a cozy, bounded island — intimate, not claustrophobic

**Report issues before proceeding to Gym 2.**

---

## 🧪 Gym 2: Farming & Tools

### ScriptableObject Assets

1. **Create Data folders**: `Assets/Data/Crops/`, `Assets/Data/Tools/`, `Assets/Data/Items/`

2. **Create CropDefinition assets** (Right-click → Create → Cloudstead/Crops/Crop Definition):
   - `Assets/Data/Crops/Turnip.asset`:
     - cropName: "Turnip", growthDays: 3
     - growSeasons: [Spring, Summer]
     - icon: `Turnip.png`
     - seedItem: → `Data/Items/TurnipSeed`
     - stageSprites: [CropSeed.png, CropSprout.png, CropGrowing.png, CropMature.png]
     - harvestOutputs: [{ item: `Data/Items/Turnip`, minYield: 1, maxYield: 1 }]
   - `Assets/Data/Crops/Potato.asset`: growthDays: 5, seedItem: PotatoSeed, harvestOutputs: [{ Potato, 1-2 }]
   - `Assets/Data/Crops/Sunflower.asset`: growthDays: 7, seedItem: SunflowerSeed, harvestOutputs: [{ Sunflower, 1-1 }, { SunflowerSeed, 0-2 }]

3. **Create ToolDefinition assets** (Right-click → Create → Cloudstead/Tools/Tool Definition):
   - `Assets/Data/Tools/Hoe.asset`: toolType = Hoe, staminaCost: 2, useTime: 0.3, maxStack: 1, icon: `ToolHoe.png`
   - `Assets/Data/Tools/WateringCan.asset`: toolType = WateringCan, staminaCost: 1, useTime: 0.2, maxStack: 1, icon: `ToolWateringCan.png`
   - `Assets/Data/Tools/Scythe.asset`: toolType = Scythe, staminaCost: 1, useTime: 0.2, maxStack: 1, icon: `ToolScythe.png`

4. **Create ItemDefinition assets** (Right-click → Create → Cloudstead/Items/Item Definition):
   - **Seeds** (category: Seed):
     - `Assets/Data/Items/TurnipSeed.asset`: itemName: "Turnip Seed", maxStack: 99, buyPrice: 20, sellPrice: 5
     - `Assets/Data/Items/PotatoSeed.asset`: buyPrice: 30, sellPrice: 8
     - `Assets/Data/Items/SunflowerSeed.asset`: buyPrice: 50, sellPrice: 15
   - **Produce** (category: General):
     - `Assets/Data/Items/Turnip.asset`: itemName: "Turnip", sellPrice: 40
     - `Assets/Data/Items/Potato.asset`: itemName: "Potato", sellPrice: 60
     - `Assets/Data/Items/Sunflower.asset`: itemName: "Sunflower", sellPrice: 100

5. **Create Soil Tile assets**:
   - Right-click → Create → Cloudstead/Soil Tile → `TilledSoil.asset`: assign `TilledSoil.png` as tilled sprite
   - Create another → `WateredSoil.asset`: assign `WateredSoil.png` as watered sprite (or use one SoilTile with both sprites)

### Scene Updates

6. **Add FarmingManager** (empty GameObject):
   - Attach `FarmingManager`
   - Assign soil tilemap reference, tile references for tilled/watered soil
   - Assign crop definitions if the manager has array fields for them

7. **Update Player prefab**:
   - Attach `ToolController`:
     - quickbarUI: will wire in Gym 3 (leave empty for now — tools won't function until inventory is set up)
     - cropRegistry: drag all CropDefinitions (Turnip, Potato, Sunflower) from `Data/Crops/`
   - Attach `PlayerAnimator`: assign Player.png as idle sprite (single sprite is fine for now)

8. **Update GameBootstrapper**: Verify it initializes FarmingManager (code already does this)

### Play Test — Farming & Tools

Enter **Play mode**:
- [x] Walk to cloud center area
- [x] Use tool (bound to UseTool action — check your input bindings, likely left-click or E)
- [x] With Hoe equipped: clicking on a ground tile changes it to tilled soil (darker tile)
- [x] With WateringCan equipped: clicking on tilled soil darkens it further (watered)
- [x] Planted crops show a seed sprite on the tilled soil
- [x] **Debug day advance**: If no bed yet, add a temporary button or use the Unity console to call `EventBus.Publish(new DayStartedEvent { Day = TimeManager.Instance?.CurrentDay ?? 1, Season = Season.Spring })`
- [x] Crops grow through stages (seed → sprout → growing → mature) over multiple days when watered
- [x] Mature crops can be harvested
- [x] Unwatered crops stay at their current growth stage (no punishment)
- [x] Tool targeting hits the correct tile based on facing direction
- [ ] Tilling sound plays on till, watering sound on water, harvest sound on harvest *(Deferred to Polish — no sounds wired yet)*

**Report issues before proceeding to Gym 3.**

---

## 🧪 Gym 3: Inventory & Items

### Scene Updates

1. **Add InventoryManager** (empty GameObject):
   - Attach `InventoryManager`

2. **Create HUD Canvas** (GameObject → UI → Canvas):
   - Canvas: Screen Space - Overlay
   - Canvas Scaler: Scale With Screen Size, Reference: 1280×720
   - Name: "HUDCanvas"

3. **Create HUD elements** (children of HUDCanvas):
   - "HUD" (empty, stretch anchors) → attach `HUDController`
     - "ClockText" (UI → Text - TextMeshPro): anchor top-center, text "Day 1 — Spring"
     - "StaminaBarBg" (UI → Image): anchor top-left, sprite `StaminaBarBg.png`, size 128×16
       - Child "StaminaBarFill" (Image): sprite `StaminaBarFill.png`, Image Type: Filled, Fill Origin: Left
     - "CurrencyText" (TMP): anchor top-right, text "0g"
   - "Quickbar" (empty, anchor bottom-center, Horizontal Layout Group, spacing: 4) → attach `QuickbarUI`
     - Create 5 × "Slot" children:
       - Each: Image (sprite: `SlotBorder.png`), attach `SlotUI`
       - Child "Icon" (Image, blank), Child "Count" (TMP, ""), Child "Highlight" (Image: `SlotHighlight.png`, starts disabled)
       - Wire SlotUI fields: iconImage, countText, highlightBorder
     - Wire QuickbarUI: drag all 5 SlotUI references into the slots array

4. **Create Inventory Panel** (child of HUDCanvas, starts **disabled**):
   - "InventoryPanel" (Image: `PanelBackground.png`, Image Type: Sliced, center-anchored, 600×400) → attach `InventoryUI`
     - "SlotsGrid" (Grid Layout Group: cell 64×64, spacing 4, constraint: Fixed Column Count = 6)
       - Create 24 × "Slot" children (same structure as quickbar slots, with `SlotUI`)
     - "CloseButton" (Button, top-right corner): text "X"
   - Wire InventoryUI: drag slot references, set close button

5. **Wire HUDController fields**: clockText, staminaBar, currencyText references

6. **Add FarmStartSetup** (empty GameObject or on GameBootstrapper):
   - Attach `FarmStartSetup`
   - Drag item assets: TurnipSeed, PotatoSeed, SunflowerSeed, Hoe, WateringCan

7. **Wire PlayerInput events**:
   - Player/OpenInventory (Tab or I) → `InventoryUI.Toggle()` (you may need to add a public Toggle method)
   - Player/QuickbarSlot1-5 → `QuickbarUI.SelectSlot()` with indices 0-4

### Play Test — Inventory & Items

Enter **Play mode**:
- [x] Starting items appear in quickbar (Hoe, WateringCan in first slots, seeds after)
- [x] Number keys 1-5 highlight different quickbar slots
- [x] Press Tab/I to open inventory — see all items in grid
- [ ] Drag-drop items to rearrange (if drag-drop is implemented) **Not implemented yet**
- [x] Close inventory with X button or Tab/I again
- [x] Select seed in quickbar → till soil → plant: seed count decreases
- [x] Harvest mature crop → crop item appears in inventory
- [x] Inventory opens/closes cleanly (game pauses/unpauses appropriately)
- [x] HUD shows day/currency text (even if placeholder values)
- [x] Quickbar shows correct item icons

**Report issues before proceeding to Gym 4.**

---

## 🧪 Gym 4: Time & Stamina

### Scene Updates

1. **Create GlobalLight** (empty GameObject):
   - Add `Light 2D` component (Component → Rendering → Light → 2D):
     - Light Type = **Global**, Color = white, Intensity = 1
     - **Note**: Requires URP 2D renderer. If `Light 2D` doesn't appear, check: Edit → Project Settings → Graphics → Scriptable Render Pipeline Settings → ensure a URP asset with 2D Renderer is assigned.
   - Attach `DayNightController`:
     - Drag the Light 2D component into the `globalLight` field
     - Leave gradient/curve fields empty — the script creates sensible defaults (warm dawn → bright noon → amber dusk → deep blue midnight)

2. **Create TimeManager** (empty GameObject):
   - Attach `TimeManager`:
     - dayLengthSeconds: **60** (fast for testing! Set to 720 for real gameplay later)

3. **Create Bed** (from existing `Assets/Prefabs/Player/Bed.prefab`, or create new):
   - SpriteRenderer: sprite = `Bed.png`, Sorting Layer = Objects, Order = 0
   - Add `Bed` component:
     - Drag the HUDController into the `hudController` field (so clock updates after sleep)
   - Add `BoxCollider2D`: Is Trigger = ✓, Layer = **Interactable**
   - Place in scene at (16, 12) — near cloud center

4. **Update Player prefab** (if not already done):
   - Attach `StaminaController`: maxStamina = 100, lowStaminaThreshold = 20
   - The stamina system is already wired into ToolController — each tool use costs `tool.staminaCost`

5. **Wire HUD** (update HUDController references if not already wired):
   - clockText → ClockText TMP object
   - staminaBar fill → StaminaBarFill Image
   - The scripts subscribe to events automatically via EventBus

6. **Update GameBootstrapper init order**: GameManager → TimeManager → InventoryManager → FarmingManager → TileManager → SaveManager (code already handles this)

### Play Test — Time & Stamina

Enter **Play mode** (with dayLengthSeconds = 60 for fast cycle):
- [x] Day/night light transitions smoothly: warm gold morning → bright noon → amber dusk → deep blue night
- [x] Clock text updates as time passes ("Day 1 — Spring")
- [x] Use tools repeatedly — stamina bar decreases (Hoe costs 2, WateringCan/Scythe cost 1)
- [x] When stamina gets low (< 20): player moves slower, console shows "*yawn*" messages
- [x] Player can STILL use tools and walk when stamina is low (never blocks — cozy!)
- [x] Walk to bed, press Interact (E): sleep triggers
- [x] After sleep: day advances, stamina fully restores, morning light returns
- [x] Crops grow on sleep (only watered crops advance)
- [x] Multiple day/night cycles look smooth
- [x] F5 still works to advance day (DebugControls), F6 restores 50 stamina
- [x] Low stamina feels like a warm "time to rest" suggestion, not a punishment

**Report issues before proceeding to Integration.**

---

## 🧪 Integration Check: Core Farming Loop

### Final Wiring

1. **Add GameStateController** (empty GameObject):
   - Attach `GameStateController`
   - Drag Player's `PlayerInput` component into the playerInput field

2. **Add CropParticles** (empty GameObject):
   - Attach `CropParticles`
   - Create 3 simple Particle Systems as children (or separate prefabs):
     - **PlantPuff**: Start Color = brown, Start Size = 0.2, Duration = 0.3, Max Particles = 5, Emission Burst = 5, Shape = Sphere (small radius)
     - **WaterDrops**: Start Color = blue, Start Size = 0.15, Duration = 0.5, Max Particles = 8, Gravity Modifier = 1, Emission Burst = 8
     - **HarvestSparkle**: Start Color = gold/yellow, Start Size = 0.2, Duration = 0.8, Max Particles = 10, Shape = Sphere, Start Speed = 1 upward, Gravity = -0.5
   - Drag each into CropParticles fields (plantPuff, waterDrops, harvestSparkle)
   - Set all three to **Play On Awake = false**

3. **Wire ToolController crop registry**:
   - On the Player's ToolController, drag all CropDefinitions (Turnip, Potato, Sunflower) into the `cropRegistry` array
   - This enables seed → crop lookup when planting

4. **Wire CropDefinition seed references**:
   - On each CropDefinition asset, set the `seedItem` field:
     - Turnip → TurnipSeed, Potato → PotatoSeed, Sunflower → SunflowerSeed
   - Set `harvestOutputs` for each crop:
     - Turnip: item = `Items/Turnip`, minYield 1, maxYield 1
     - Potato: item = `Items/Potato`, minYield 1, maxYield 2
     - Sunflower: output 1 = `Items/Sunflower` (1-1), output 2 = `Items/SunflowerSeed` (0-2)

3. **Verify complete scene hierarchy**:
   ```
   Farm Scene
   ├── GameBootstrapper
   ├── GameManager
   ├── TimeManager
   ├── InventoryManager
   ├── FarmingManager (tilledSoilTile, wateredSoilTile refs)
   ├── SaveManager
   ├── TileManager (3× tilemap refs)
   ├── GameStateController (playerInput ref)
   ├── DebugControls (F5=advance day, F6=restore stamina)
   ├── FarmStartSetup (seed + tool SO refs)
   ├── CropParticles
   │   ├── PlantPuff (ParticleSystem)
   │   ├── WaterDrops (ParticleSystem)
   │   └── HarvestSparkle (ParticleSystem)
   ├── GlobalLight (Light2D + DayNightController)
   ├── Grid
   │   ├── GroundTilemap
   │   ├── SoilTilemap
   │   └── DecorationTilemap
   ├── CloudGeneration (CloudGenerator + CloudBoundary)
   ├── Player (prefab — PlayerController, ToolController, StaminaController, PlayerInput)
   ├── Bed (Bed + BoxCollider2D trigger)
   ├── MainCamera (CameraController)
   └── HUDCanvas
       ├── HUD (HUDController)
       │   ├── ClockText
       │   ├── StaminaBarBg / StaminaBarFill
       │   └── CurrencyText
       ├── Quickbar (QuickbarUI + 5× SlotUI)
       └── InventoryPanel (InventoryUI + 24× SlotUI, starts disabled)
   ```

### Full Integration Test

Set `dayLengthSeconds = 120` for comfortable pace. Play for **5+ in-game days**:

- [x] Start with seeds and tools in inventory (quickbar shows Hoe, WateringCan, Scythe, seeds)
- [x] Till soil with Hoe (stamina decreases by 2)
- [x] Plant seed from inventory (seed consumed from quickbar, crop sprite appears)
- [x] Water with WateringCan (soil darkens, stamina decreases by 1)
- [x] Sleep in bed → day advances, stamina restores, morning light
- [x] Crops grow (only watered ones advance stages)
- [x] Harvest mature crops with Scythe → items appear in inventory
- [x] Skip watering for 2+ days → crops stay at current stage (no wilting, no death — cozy!)
- [x] Open/close inventory with Tab — game pauses/unpauses
- [x] Switch tools via quickbar (1-5 keys), quickbar highlight updates
- [x] Stamina runs low → player slows, yawns; can still walk to bed and use tools
- [x] Day/night cycle looks smooth across multiple sleeps
- [x] Input maps switch correctly: can't move while inventory is open, can't open inventory while sleeping
- [x] **The loop feels like "one more day"** — the core cozy farming experience

---

## ⚠️ CORE GATE — ✅ PASSED

**Feedback:**
- Farming feels similar enough to genre — satisfactory
- Cloud doesn't feel like home yet — needs house structure and visual warmth
- Day/night cycle is a nice touch
- Core loop is enjoyable, needs more content for depth
- Low stamina slowdown not visually obvious — needs bar pulse + (de)buff icons
- #1 request: house and buy/sell ability

**Bug fixes applied:**
- ✅ Stamina only restores on bed sleep (PlayerSleptEvent)
- ✅ Watered empty tiles dry overnight
- ✅ Mouse-based tile targeting within Chebyshev range
- ✅ Configurable interaction range on GameManager
- ✅ Player feet offset for tile sampling

**Proceeding to Iteration 2.**

---

## 🧪 Gym 5: NPC & Shop

### Scene Updates

1. **Add EconomyManager** (empty GameObject):
   - Attach `EconomyManager`: startingGold = 500

2. **Add DialogueManager** (empty GameObject):
   - Attach `DialogueManager`

3. **Add ShopManager** (empty GameObject):
   - Attach `ShopManager`
   - Wire `shopUI` field → drag the ShopUI component (created in step 8 below)

4. **Create NPC ScriptableObjects** (Right-click → Create → Cloudstead/NPC/NPC Definition in `Assets/Data/NPCs/`):
   - `Merchant_Mabel.asset`: npcName="Mabel", isMerchant=true, greetings=["Welcome to my shop, dear!", "Looking for seeds?"], shopInventory=[TurnipSeed, PotatoSeed, SunflowerSeed], shopPrices=[20, 30, 50]
   - `Villager_Elm.asset`: npcName="Elm", isMerchant=false, greetings=["Beautiful day up here in the clouds!"]

5. **Create DialogueTree assets** (Right-click → Create → Cloudstead/NPC/Dialogue Tree in `Assets/Data/Dialogues/`):
   - `Mabel_Greeting.asset`: Node 0: speakerName="Mabel", text="Welcome! Want to see what I have?", choiceTexts=["Yes, show me!", "Just saying hello!"], choiceNextIndices=[-1, 1] → choice 0 (index -1) ends dialogue and opens shop (merchant auto-detected); choice 1 → Node 1: speakerName="Mabel", text="Have a lovely day, dear!", nextIndex=-1
   - `Elm_Chat.asset`: Node 0: speakerName="Elm", text="Beautiful day up here!", nextIndex=1 → Node 1: speakerName="Elm", text="I've been thinking about growing sunflowers.", nextIndex=-1

6. **Create NPC prefabs** (`Assets/Prefabs/NPCs/`):
   - "Mabel" prefab: SpriteRenderer (Sorting Layer: Characters), `NPCController` (definition: Merchant_Mabel, greetingDialogue: Mabel_Greeting), BoxCollider2D (Is Trigger: true, Layer: Interactable)
   - "Elm" prefab: same structure, definition: Villager_Elm, greetingDialogue: Elm_Chat
   - **Important**: For merchants, always set `greetingDialogue` so the player sees dialogue before the shop opens. If `greetingDialogue` is empty, the shop opens immediately on interact.

7. **Place NPCs in Farm scene** (for gym testing — no Town scene needed yet):
   - Mabel at a comfortable position near cloud center
   - Elm elsewhere on the cloud

8. **Create Dialogue Canvas** (child of HUDCanvas):
   - "DialoguePanel" (anchor: bottom, 800×200 warm brown panel): attach `DialogueUI`
     - "PortraitImage" (left, 128×128 Image)
     - "NameText" (TMP, above dialogue)
     - "DialogueText" (TMP, main area, word-wrapped)
     - "ChoiceContainer" (empty with Vertical Layout Group)
     - "ContinuePrompt" (TMP, "Press E to continue...", blinks gently)
   - Wire DialogueUI fields: dialoguePanel, portraitImage, nameText, dialogueText, choiceContainer, continuePrompt
   - Create **ChoiceButton prefab**: a UI Button with a child TextMeshProUGUI. Drag into `choiceButtonPrefab` field.
   - Dialogue advances when the player presses **E** (hardcoded in DialogueUI.Update via InputSystem).

9. **Create Shop Canvas** (child of HUDCanvas):
   - "ShopPanel" (center, 800×500, starts **disabled**): attach `ShopUI`
     - "ShopSide" (left, Vertical Layout Group): `shopItemsContainer`
     - "PlayerSide" (right, Vertical Layout Group): `playerItemsContainer`
     - "GoldDisplay" (bottom center, TMP): wire to `goldText`
     - "CloseButton" (Button, top-right): wire to `closeButton`
   - Create **ShopItemPrefab**: Button + 2× TMP children (item name, price). Drag into `shopItemPrefab`.
   - Create **PlayerItemPrefab**: Button + 2× TMP children (item name + count, sell price). Drag into `playerItemPrefab`.
   - **Wire ShopManager**: Go back to the ShopManager GameObject, drag the ShopUI component into its `shopUI` field.

10. **Update HUDController**: Wire currency display to `GoldChangedEvent`

11. **Verify scene hierarchy** (new objects for this gym):
    ```
    Farm Scene (additions)
    ├── EconomyManager
    ├── DialogueManager
    ├── ShopManager (shopUI → ShopUI component)
    ├── Mabel (NPCController + BoxCollider2D trigger)
    ├── Elm (NPCController + BoxCollider2D trigger)
    └── HUDCanvas
        ├── DialoguePanel (DialogueUI, starts disabled)
        │   ├── PortraitImage
        │   ├── NameText
        │   ├── DialogueText
        │   ├── ChoiceContainer
        │   └── ContinuePrompt
        └── ShopPanel (ShopUI, starts disabled)
            ├── ShopSide (shopItemsContainer)
            ├── PlayerSide (playerItemsContainer)
            ├── GoldDisplay
            └── CloseButton
    ```

### Play Test — NPC & Shop

Enter **Play mode**:
- [ ] Walk to Mabel, press E → dialogue opens with typewriter text
- [ ] Choose to shop → shop UI opens with items and prices
- [ ] Buy seeds (gold deducted, item added to inventory)
- [ ] Close shop, walk to Elm, press E → friendly dialogue plays
- [ ] Sell crops from inventory (gold increases, item removed)
- [ ] Gold display updates in real time on HUD
- [ ] Dialogue UI is warm-toned and non-intrusive
- [ ] Shop prices feel fair and generous (500 gold buys plenty)

**Report issues before proceeding.**

---

## 🔊 SFX & Juice Pass

### Setup

1. Run `node Tools/generate_sounds.js` → verify WAVs in `Assets/Audio/SFX/`
2. Import WAVs → set compression to PCM (short clips)
3. Add `AudioSource` component to: Player, Bed, CloudBoundary root

### Wiring

| Script | Field | Audio Clip |
|--------|-------|-----------|
| ToolController | tillSound | till_soil.wav |
| ToolController | waterSound | water_pour.wav |
| ToolController | harvestSound | harvest_pop.wav |
| ToolController | plantSound | plant_seed.wav |
| InventoryManager | itemPickupSound | item_pickup.wav |
| InventoryUI | openSound | ui_open.wav |
| InventoryUI | closeSound | ui_close.wav |
| Bed | sleepJingle | sleep_jingle.wav |
| CloudBoundary | edgeBumpSound | edge_bump.wav |
| StaminaController | yawnSound | yawn.wav |

### Stamina Bar Polish

4. On HUD, the StaminaBarFill Image should pulse (gentle alpha oscillation) when stamina < lowStaminaThreshold
5. Add "StatusIcons" horizontal layout above StaminaBarBg → attach `StatusIconUI`
6. Create 16×16 icon sprites: `Tired.png` (sleepy face), `Rested.png` (sparkle)
7. Wire StatusIconUI: shows "Tired" when stamina low, briefly shows "Rested" after sleeping

### Test

- [ ] Every tool action produces a distinct sound
- [ ] Plant, open/close inventory, sleep all produce sounds
- [ ] Cloud edge contact produces soft bump
- [ ] Low stamina → bar pulses, "Tired" icon appears, yawn sound
- [ ] After sleep → bar stops pulsing, brief "Rested" icon

---

## 🏠 House Structure

1. Create/import house sprite (`Assets/Art/Sprites/House.png`, ~96×96 px for 3×3 tiles at 32ppu)
2. Create "House" GameObject in Farm scene: SpriteRenderer (Sorting Layer: Objects, Order: 0)
3. Position near cloud center
4. Move Bed to be adjacent to the house (at the "door" side)
5. House is purely visual for now — no collision, no interior

### Test

- [ ] Cloud feels more like home with a house structure
- [ ] Bed placement makes sense next to house

---

## All-in-1 Sprite Shader (Optional Juice)

After importing the All-in-1 Sprite Shader package from the Asset Store:

- **Interactable Outline**: Add `AllIn1SpriteShader` to the Bed prefab and any interactable objects. Enable **Outline** effect (warm gold color, width 1-2px). Toggle via script when player is in interaction range.
- **Crop Glow**: On mature crop sprites, enable **Glow** effect (soft green) to indicate they're ready to harvest.
- **Low Stamina Vignette**: Apply **Color Tint** (warm amber) to the player sprite when stamina is low.
- **Fade/Dissolve**: Use the **Fade** effect for the sleep transition instead of/alongside screen fade.
- **Hologram/Ghost**: Reserve for future building placement preview.
