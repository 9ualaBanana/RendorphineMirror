namespace _3DProductsPublish.CGTrader;

public partial class CGTrader
{
    public partial record _3DProduct
    {
        public partial record Metadata__
        {
            public record Category_
            {
                public static Category_ Default { get; } = Scanned3DModels(Scanned3DModelsSubCategory.Various);

                public readonly Metadata_.Category_ Category;
                public readonly Metadata_.Category_ SubCategory;

                Category_(Metadata_.Category_ category, Metadata_.Category_ subCategory) =>
                    (Category, SubCategory) = (category, subCategory);


                internal static Category_ Parse(string category) =>
                    Enum.TryParse<AircraftSubCategory>(category, true, out var aircraftSubCategory) ? Aircraft(aircraftSubCategory) :
                    Enum.TryParse<AnimalsSubCategory>(category, true, out var animalsSubCategory) ? Animals(animalsSubCategory) :
                    Enum.TryParse<ArchitecturalSubCategory>(category, true, out var architecturalSubCategory) ? Architectural(architecturalSubCategory) :
                    Enum.TryParse<ExteriorSubCategory>(category, true, out var exteriorSubCategory) ? Exterior(exteriorSubCategory) :
                    Enum.TryParse<InteriorSubCategory>(category, true, out var interiorSubCategory) ? Interior(interiorSubCategory) :
                    Enum.TryParse<CarSubCategory>(category, true, out var carSubCategory) ? Car(carSubCategory) :
                    Enum.TryParse<CharacterSubCategory>(category, true, out var characterSubCategory) ? Character(characterSubCategory) :
                    Enum.TryParse<ElectronicsSubCategory>(category, true, out var electronicsSubCategory) ? Electoronics(electronicsSubCategory) :
                    Enum.TryParse<FoodSubCategory>(category, true, out var foodSubCategory) ? Food(foodSubCategory) :
                    Enum.TryParse<FurnitureSubCategory>(category, true, out var furnitureSubCategory) ? Furniture(furnitureSubCategory) :
                    Enum.TryParse<HouseholdSubCategory>(category, true, out var householdSubCategory) ? Household(householdSubCategory) :
                    Enum.TryParse<IndustrialSubCategory>(category, true, out var industrialSubCategory) ? Industrial(industrialSubCategory) :
                    Enum.TryParse<PlantSubCategory>(category, true, out var plantSubCategory) ? Plant(plantSubCategory) :
                    Enum.TryParse<ScienceSubCategory>(category, true, out var scienceSubCategory) ? Science(scienceSubCategory) :
                    Enum.TryParse<SpaceSubCategory>(category, true, out var spaceSubCategory) ? Space(spaceSubCategory) :
                    Enum.TryParse<SportsSubCategory>(category, true, out var sportsSubCategory) ? Sports(sportsSubCategory) :
                    Enum.TryParse<VehicleSubCategory>(category, true, out var vehicleSubCategory) ? Vehicle(vehicleSubCategory) :
                    Enum.TryParse<WatercraftSubCategory>(category, true, out var watercraftSubCategory) ? Watercraft(watercraftSubCategory) :
                    Enum.TryParse<MilitarySubCategory>(category, true, out var militarySubCategory) ? Military(militarySubCategory) :
                    Enum.TryParse<Scanned3DModelsSubCategory>(category, true, out var scanned3DModelsSubCategory) ? Scanned3DModels(scanned3DModelsSubCategory) :
                    Enum.TryParse<ScriptsAndPluginsSubCategory>(category, true, out var scriptsAndPluginsSubCategory) ? ScriptsAndPlugins(scriptsAndPluginsSubCategory) :
                    Enum.TryParse<EngineeringPartsSubCategory>(category, true, out var engineeringPartsSubCategory) ? EngineeringParts(engineeringPartsSubCategory) :
                    Enum.TryParse<VariousSubCategory>(category, true, out var variousSubCategory) ? Various(variousSubCategory) :
                    Enum.TryParse<TexturesSubCategory>(category, true, out var texturesSubCategory) ? Textures(texturesSubCategory) :
                    Default;

                public static Category_ Aircraft(AircraftSubCategory subCategory) => new(new(Enum.GetName(Category__.Aircraft)!, 1), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Animals(AnimalsSubCategory subCategory) => new(new(Enum.GetName(Category__.Animals)!, 10), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Architectural(ArchitecturalSubCategory subCategory) => new(new(Enum.GetName(Category__.Architectural)!, 18), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Exterior(ExteriorSubCategory subCategory) => new(new(Enum.GetName(Category__.Exterior)!, 27), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Interior(InteriorSubCategory subCategory) => new(new(Enum.GetName(Category__.Interior)!, 40), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Car(CarSubCategory subCategory) => new(new(Enum.GetName(Category__.Car)!, 50), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Character(CharacterSubCategory subCategory) => new(new(Enum.GetName(Category__.Character)!, 58), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Electoronics(ElectronicsSubCategory subCategory) => new(new(Enum.GetName(Category__.Electoronics)!, 67), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Food(FoodSubCategory subCategory) => new(new(Enum.GetName(Category__.Food)!, 73), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Furniture(FurnitureSubCategory subCategory) => new(new(Enum.GetName(Category__.Furniture)!, 78), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Household(HouseholdSubCategory subCategory) => new(new(Enum.GetName(Category__.Household)!, 98), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Industrial(IndustrialSubCategory subCategory) => new(new(Enum.GetName(Category__.Industrial)!, 102), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Plant(PlantSubCategory subCategory) => new(new(Enum.GetName(Category__.Plant)!, 107), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Science(ScienceSubCategory subCategory) => new(new(Enum.GetName(Category__.Science)!, 115), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Space(SpaceSubCategory subCategory) => new(new(Enum.GetName(Category__.Space)!, 119), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Sports(SportsSubCategory subCategory) => new(new(Enum.GetName(Category__.Sports)!, 124), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Vehicle(VehicleSubCategory subCategory) => new(new(Enum.GetName(Category__.Vehicle)!, 130), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Watercraft(WatercraftSubCategory subCategory) => new(new(Enum.GetName(Category__.Watercraft)!, 141), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Military(MilitarySubCategory subCategory) => new(new(Enum.GetName(Category__.Military)!, 147), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Scanned3DModels(Scanned3DModelsSubCategory subCategory) => new(new(Enum.GetName(Category__.Scanned3DModels)!, 147), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ ScriptsAndPlugins(ScriptsAndPluginsSubCategory subCategory) => new(new(Enum.GetName(Category__.ScriptsAndPlugins)!, 230), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ EngineeringParts(EngineeringPartsSubCategory subCategory) => new(new(Enum.GetName(Category__.EngineeringParts)!, 237), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Various(VariousSubCategory subCategory) => new(new(Enum.GetName(Category__.Various)!, 315), new(Enum.GetName(subCategory)!, (int)subCategory));
                public static Category_ Textures(TexturesSubCategory subCategory) => new(new(Enum.GetName(Category__.Textures)!, 849), new(Enum.GetName(subCategory)!, (int)subCategory));
            }

            enum Category__
            {
                Aircraft = 1,
                Animals = 10,
                Architectural = 18,
                Exterior = 27,
                Interior = 40,
                Car = 50,
                Character = 58,
                Electoronics = 67,
                Food = 73,
                Furniture = 78,
                Household = 98,
                Industrial = 102,
                Plant = 107,
                Science = 115,
                Space = 119,
                Sports = 124,
                Vehicle = 130,
                Watercraft = 141,
                Military = 147,
                Scanned3DModels = 147,
                ScriptsAndPlugins = 230,
                EngineeringParts = 237,
                Various = 315,
                Textures = 849
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

        }
    }
}
