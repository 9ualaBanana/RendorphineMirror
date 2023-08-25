namespace _3DProductsPublish._3DProductDS;

public partial record _3DProduct
{
    public record Metadata_
    {
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required string[] Tags
        {
            get => _tags;
            init
            {
                if (value.Length < 1)
                    throw new ArgumentOutOfRangeException(nameof(value.Length), value.Length, $"At least 5 tags are required.");
                _tags = value;
            }
        }
        string[] _tags = null!;
        public required double Price { get; init; }
        public required License_ License { get; init; }
        public Geometry_? Geometry { get; init; } = default;
        public required int Polygons
        {
            get => _polygons;
            init => _polygons = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Polygons), "Must be greater than 0");
        }
        int _polygons;
        public required int Vertices
        {
            get => _vertices;
            init => _vertices = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Vertices), "Must be greater than 0");
        }
        int _vertices;
        public bool Animated { get; } = false;
        public bool Rigged { get; } = false;
        public bool Textures { get; } = false;
        public bool Materials { get; } = false;
        public bool UVMapped { get; } = false;
        public UnwrappedUVs_? UnwrappedUVs { get; } = default;
        public bool? GameReady { get; } = default;
        public bool? PhysicallyBasedRendering { get; } = default;
        public bool? AdultContent { get; } = default;
        public bool? Collection { get; } = default;
        public bool PluginsUsed { get; } = false;


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
