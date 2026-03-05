public enum Season { Spring, Summer, Autumn, Winter }
public enum GameState { Playing, Paused, Dialogue, Menu, Airship, Sleeping, Crafting, Building, Cutscene }
public enum ItemCategory { General, Seed, CraftingMaterial }
public enum ToolType { Hoe, WateringCan, Axe, Pickaxe, Scythe }
public enum SoilState { Untilled, Tilled, Watered }
public enum CropStage { Seed, Sprout, Growing, Mature }
public enum Direction { Up, Down, Left, Right }
public enum AirshipTilemapLayer { Floor, Decoration, Walls }

[System.Flags]
public enum PlacementZone
{
    None     = 0,
    Exterior = 1 << 0,
    Interior = 1 << 1,
    Airship  = 1 << 2,
    All      = Exterior | Interior | Airship
}
