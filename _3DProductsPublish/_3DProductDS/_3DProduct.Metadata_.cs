namespace _3DProductsPublish._3DProductDS;

public partial record _3DProduct
{
    // Base metadata type for deserialization from `_Submit.json`.
    public record Metadata_
    {
        [JsonProperty("toSubmitSquid")] public required string StatusSquid { get; init; }
        [JsonProperty("toSubmitTrader")] public required string StatusTrader { get; init; }
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required string Category { get; init; }
        public string? SubCategory { get; init; }
        public required string[] Tags
        {
            get => _tags;
            init
            {
                if (value.Length < 1)
                    throw new ArgumentOutOfRangeException(nameof(value.Length), value.Length, $"At least 1 tag is required.");
                _tags = value;
            }
        }
        [JsonIgnore] string[] _tags = null!;
        public required double PriceSquid { get; init; }
        public required double PriceTrader { get; init; }
        public required License_ License { get; init; }
        public Geometry_? Geometry { get; init; } = default;
        public required int Polygons
        {
            get => _polygons;
            init => _polygons = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Polygons), "Must be greater than 0");
        }
        [JsonIgnore] int _polygons;
        public required int Vertices
        {
            get => _vertices;
            init => _vertices = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Vertices), "Must be greater than 0");
        }
        [JsonIgnore] int _vertices;
        public bool Animated { get; init; } = false;
        public bool Collection { get; init; } = false;
        public bool Rigged { get; init; } = false;
        public bool Textures { get; init; } = false;
        public bool Materials { get; init; } = false;
        public bool UVMapped { get; init; } = false;
        public UnwrappedUVs_? UnwrappedUVs { get; init; } = default;
        public bool GameReady { get; init; } = false;
        public bool PhysicallyBasedRendering { get; init; } = false;
        public bool AdultContent { get; init; } = false;
        public bool PluginsUsed { get; init; } = false;


        public record struct Category_(string Name, int ID);

        public enum License_ { RoyaltyFree, Editorial }

        public enum Geometry_
        {
            PolygonalQuadsOnly,
            PolygonalQuadsTris,
            PolygonalTrisOnly,
            PolygonalNgonsUsed,
            Polygonal,
            Subdivision,
            Nurbs,
            Unknown
        }

        public enum UnwrappedUVs_
        {
            NonOverlapping,
            Overlapping,
            Mixed,
            No,
            Unknown
        }
    }
}
