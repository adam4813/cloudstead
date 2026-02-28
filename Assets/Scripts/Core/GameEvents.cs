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
public struct AirshipLandedEvent { public string CloudName; }
