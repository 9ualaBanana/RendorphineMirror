namespace _3DProductsPublish.CGTrader._3DModelComponents;

public record CGTrader3DProductCategory
{
    public readonly int CategoryID;
    public readonly int SubCategoryID;

    CGTrader3DProductCategory(int categoryId, int subCategoryId) =>
        (CategoryID, SubCategoryID) = (categoryId, subCategoryId);

    public static CGTrader3DProductCategory Aircraft(AircraftSubCategory subCategory) => new(1, (int)subCategory);
    public static CGTrader3DProductCategory Animals(AnimalsSubCategory subCategory) => new(10, (int)subCategory);
    public static CGTrader3DProductCategory Architectural(ArchitecturalSubCategory subCategory) => new(18, (int)subCategory);
    public static CGTrader3DProductCategory Exterior(ExteriorSubCategory subCategory) => new(27, (int)subCategory);
    public static CGTrader3DProductCategory Interior(InteriorSubCategory subCategory) => new(40, (int)subCategory);
    public static CGTrader3DProductCategory Car(CarSubCategory subCategory) => new(50, (int)subCategory);
    public static CGTrader3DProductCategory Character(CharacterSubCategory subCategory) => new(58, (int)subCategory);
    public static CGTrader3DProductCategory Electoronics(ElectronicsSubCategory subCategory) => new(67, (int)subCategory);
    public static CGTrader3DProductCategory Food(FoodSubCategory subCategory) => new(73, (int)subCategory);
    public static CGTrader3DProductCategory Furniture(FurnitureSubCategory subCategory) => new(78, (int)subCategory);
    public static CGTrader3DProductCategory Household(HouseholdSubCategory subCategory) => new(98, (int)subCategory);
    public static CGTrader3DProductCategory Industrial(IndustrialSubCategory subCategory) => new(102, (int)subCategory);
    public static CGTrader3DProductCategory Plant(PlantSubCategory subCategory) => new(107, (int)subCategory);
    public static CGTrader3DProductCategory Science(ScienceSubCategory subCategory) => new(115, (int)subCategory);
    public static CGTrader3DProductCategory Space(SpaceSubCategory subCategory) => new(119, (int)subCategory);
    public static CGTrader3DProductCategory Sports(SportsSubCategory subCategory) => new(124, (int)subCategory);
    public static CGTrader3DProductCategory Vehicle(VehicleSubCategory subCategory) => new(130, (int)subCategory);
    public static CGTrader3DProductCategory Watercraft(WatercraftSubCategory subCategory) => new(141, (int)subCategory);
    public static CGTrader3DProductCategory Military(MilitarySubCategory subCategory) => new(147, (int)subCategory);
    public static CGTrader3DProductCategory Scanned3DModels(Scanned3DModelsSubCategory subCategory) => new(147, (int)subCategory);
    public static CGTrader3DProductCategory ScriptsAndPlugins(ScriptsAndPluginsSubCategory subCategory) => new(230, (int)subCategory);
    public static CGTrader3DProductCategory EngineeringParts(EngineeringPartsSubCategory subCategory) => new(237, (int)subCategory);
    public static CGTrader3DProductCategory Various(VariousSubCategory subCategory) => new(315, (int)subCategory);
    public static CGTrader3DProductCategory Textures(TexturesSubCategory subCategory) => new(849, (int)subCategory);
}

public enum AircraftSubCategory
{
    Part = 2,
    Commercial = 3,
    Helicopter = 4,
    Historic = 5,
    Jet = 6,
    Military = 7,
    Other = 8,
    Private = 9
}

public enum AnimalsSubCategory
{
    Bird = 11,
    Dinosaur = 12,
    Fish = 13,
    Insect = 14,
    Mammal = 15,
    Other = 16,
    Reptile = 17
}

public enum ArchitecturalSubCategory
{
    Engineering = 19,
    Decoration = 20,
    Door = 21,
    Fixture = 22,
    Floor = 23,
    Lighting = 24,
    Other = 25,
    Street = 26,
    Window = 847
}

public enum ExteriorSubCategory
{
    Stadium = 28,
    Cityscape = 29,
    Office = 30,
    Historic = 31,
    House = 32,
    Industrial = 33,
    Landmark = 34,
    Landscape = 35,
    Other = 36,
    SciFi = 37,
    Skyscraper = 38,
    Street = 39,
    Public = 155
}

public enum InteriorSubCategory
{
    Bathroom = 41,
    Bedroom = 42,
    Hall = 43,
    House = 44,
    Kitchen = 46,
    LivingRoom = 47,
    Office = 48,
    Other = 49
}

public enum CarSubCategory
{
    Antique = 51,
    Concept = 52,
    SUV = 53,
    Luxury = 54,
    Racing = 55,
    Sport = 56,
    Standard = 57
}

public enum CharacterSubCategory
{
    Anatomy = 59,
    Child = 60,
    Clothing = 61,
    Fantasy = 62,
    Man = 63,
    Other = 64,
    SciFi = 65,
    Woman = 66
}

public enum ElectronicsSubCategory
{
    Audio = 68,
    Computer = 69,
    Other = 70,
    Phone = 71,
    Video = 72
}

public enum FoodSubCategory
{
    Beverage = 74,
    Fruit = 75,
    Other = 76,
    Vegetables = 77
}

public enum FurnitureSubCategory
{
    Appliance = 79,
    Bed = 80,
    Cabinet = 81,
    Chair = 82,
    Outdoor = 83,
    Kitchen = 84,
    Lamp = 85,
    Other = 86,
    Sofa = 87,
    Table = 88,
    TablewareOrFurnitureSet = 89,
}

public enum HouseholdSubCategory
{
    Kitchenware = 99,
    Other = 100,
    Tools = 101
}

public enum IndustrialSubCategory
{
    Machine = 103,
    Other = 104,
    Part = 105,
    Tool = 106
}

public enum PlantSubCategory
{
    Conifer = 108,
    Flower = 109,
    Grass = 110,
    Leaf = 111,
    Other = 112,
    PotPlant = 113,
    Bush = 114
}

public enum ScienceSubCategory
{
    Laboratory = 116,
    Medical = 117,
    Other = 118
}

public enum SpaceSubCategory
{
    Other = 120,
    Planet = 121,
    Spaceship = 122
}

public enum SportsSubCategory
{
    Game = 125,
    Book = 126,
    Equipment = 127,
    Music = 128,
    Toy = 129
}

public enum VehicleSubCategory
{
    Bicycle = 131,
    Bus = 132,
    Industrial = 133,
    Military = 134,
    Motorcycle = 135,
    Other = 136,
    Part = 137,
    SciFi = 138,
    Train = 139,
    Truck = 140
}

public enum WatercraftSubCategory
{
    Historic = 142,
    Industrial = 143,
    Military = 144,
    Other = 145,
    Recreational = 146
}

public enum MilitarySubCategory
{
    Armor = 148,
    Character = 149,
    Gun = 150,
    Melee = 151,
    Other = 152,
    Rocketry = 153,
    Vehicle = 154
}

public enum Scanned3DModelsSubCategory
{
    Various = 229
}

public enum ScriptsAndPluginsSubCategory
{
    Modelling = 231,
    Animation = 232,
    Rendering = 233,
    Lighting = 234,
    Texturing = 235,
    VFX = 236
}

public enum EngineeringPartsSubCategory
{
    ACCS = 238,
    APETechnologies = 239,
    ABdostecDosiertechnik = 240,
    AdaptedSolutions = 241,
    AerauliqaSrl = 242,
    AIGER = 243,
    AIRAP = 244,
    Aircalo = 245,
    Airtec = 246,
    ANVER = 247,
    AtlasCopco = 248,
    AVMAutomation = 249,
    AZULY = 250,
    BR = 251,
    BalmoralTanks = 252,
    Baumer = 253,
    BeckHeun = 254,
    BelgoBekaert = 255,
    BergSteelSA = 256,
    BmiAxelent = 257,
    BOLLHOFF = 258,
    cabur = 259,
    CAMOZZI = 260
}

public enum VariousSubCategory
{
    VariousModels
}

public enum TexturesSubCategory
{
    Architectural = 850,
    Natural = 851,
    Decal = 852,
    Miscellaneous = 853
}
