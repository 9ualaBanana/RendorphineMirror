using _3DProductsPublish.CGTrader._3DModelComponents;

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

public static class _3DProductMetadataExtensions
{
    public static _3DProduct<CGTrader3DProductMetadata> WithCGTrader(this _3DProduct _3DProduct, _3DProduct.Metadata_ _)
    {
        var cgTraderMetadata = CGTrader3DProductMetadata.ForCG(
            _.Title,
            _.Description,
            _.Tags,
            Category(),
            License(),
            _.PriceTrader,
            _.GameReady,
            _.Animated,
            _.Rigged,
            _.PhysicallyBasedRendering,
            _.AdultContent,
            new(_.Polygons, _.Vertices, Geometry(), _.Collection, _.Textures, _.Materials, _.PluginsUsed, _.UVMapped, UnwrappedUVs())
            );
        return new _3DProduct<CGTrader3DProductMetadata>(_3DProduct, cgTraderMetadata);


        CGTrader3DProductCategory Category()
        {
            return CGTrader3DProductCategory.Electoronics(ElectronicsSubCategory.Computer);
            throw new NotImplementedException();
        }

        NonCustomCGTraderLicense License() => _.License switch
        {
            _3DProduct.Metadata_.License_.RoyaltyFree => NonCustomCGTraderLicense.royalty_free,
            _3DProduct.Metadata_.License_.Editorial => NonCustomCGTraderLicense.editorial,
            _ => throw new NotImplementedException()
        };

        Geometry_? Geometry() => _.Geometry switch
        {
            _3DProduct.Metadata_.Geometry_.PolygonalQuadsOnly or
            _3DProduct.Metadata_.Geometry_.PolygonalQuadsTris or
            _3DProduct.Metadata_.Geometry_.PolygonalTrisOnly or
            _3DProduct.Metadata_.Geometry_.PolygonalNgonsUsed or
            _3DProduct.Metadata_.Geometry_.Polygonal => Geometry_.polygonal_mesh,
            _3DProduct.Metadata_.Geometry_.Subdivision => Geometry_.subdivision_ready,
            _3DProduct.Metadata_.Geometry_.Nurbs => Geometry_.nurbs,
            _3DProduct.Metadata_.Geometry_.Unknown => Geometry_.other,
            null => null,
            _ => throw new NotImplementedException()
        };

        UnwrappedUVs_? UnwrappedUVs() => _.UnwrappedUVs switch
        {
            _3DProduct.Metadata_.UnwrappedUVs_.NonOverlapping => UnwrappedUVs_.non_overlapping,
            _3DProduct.Metadata_.UnwrappedUVs_.Overlapping => UnwrappedUVs_.overlapping,
            _3DProduct.Metadata_.UnwrappedUVs_.Mixed => UnwrappedUVs_.mixed,
            _3DProduct.Metadata_.UnwrappedUVs_.No => UnwrappedUVs_.no,
            _3DProduct.Metadata_.UnwrappedUVs_.Unknown => UnwrappedUVs_.unknown,
            null => null,
            _ => throw new NotImplementedException()
        };
    }
}
