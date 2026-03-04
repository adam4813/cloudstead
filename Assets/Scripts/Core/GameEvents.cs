using UnityEngine;

// Forward references:
// - ItemDefinition : ScriptableObject (Assets/Scripts/Items/)
// - CropDefinition : ScriptableObject (Assets/Scripts/Items/)

public struct DayStartedEvent { public int Day; public Season Season; }
public struct DayEndedEvent { public int Day; public Season Season;}
public struct TimeTickEvent { public float NormalizedTime; }
public struct SeasonChangedEvent { public Season NewSeason; }
public struct StaminaChangedEvent { public float Current; public float Max; }
public struct InventoryChangedEvent { }
public struct ItemPickedUpEvent { public ItemDefinition Item; public int Count; }
public struct CropHarvestedEvent { public CropDefinition Crop; }
public struct CropPlantedEvent { public CropDefinition Crop; public Vector3Int Position; }
public struct CropWateredEvent { public Vector3Int Position; }
public struct CropGrownEvent { public CropDefinition Crop; public CropStage NewStage; }
public struct InteractionEvent { public GameObject Target; }
public struct GameStateChangedEvent { public GameState Previous; public GameState Current; }
public struct SceneTransitionEvent { public string TargetScene; }
public struct GoldChangedEvent { public int NewAmount; }
public struct DialogueStartedEvent { public string SpeakerName; }
public struct DialogueEndedEvent { }
public struct ItemBoughtEvent { public ItemDefinition Item; public int Price; }
public struct ItemSoldEvent { public ItemDefinition Item; public int Price; }
public struct ItemCraftedEvent { public RecipeDefinition Recipe; }
public struct SceneTransitionStartedEvent { public string TargetScene; }
public struct SceneTransitionCompletedEvent { public string Scene; }
public struct PlayerSleptEvent { }
public struct AirshipBoardedEvent { public Transform AirshipTransform; }
public struct AirshipLandedEvent { public string CloudName; }
public struct ShopGreetingEvent { public NPCDefinition Merchant; public string Greeting; }
public struct ItemPlacedEvent { public ItemDefinition Item; public Vector3Int GridPosition; }
public struct ItemRemovedFromWorldEvent { public ItemDefinition Item; public Vector3Int GridPosition; }
public struct ResourceNodeDepletedEvent { public ResourceNodeDefinition Definition; }
public struct InteriorEnteredEvent { public string BuildingId; }
public struct InteriorExitedEvent { public string BuildingId; }
public struct GiftGivenEvent { public uint GiverId; public NPCDefinition NPC; public ItemDefinition Item; }
public struct AirshipBuildModeEnteredEvent { public string AirshipId; }
public struct AirshipBuildModeExitedEvent { public string AirshipId; }
public struct AirshipTilePlacedEvent { public string AirshipId; public Vector3Int GridPosition; public AirshipTilemapLayer Layer; }
public struct WorldSettingsChangedEvent { public WorldSettings Settings; }
