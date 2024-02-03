using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.Turbosquid._3DModelComponents;

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
        [JsonIgnore] string[] _tags = null!;
        public required double Price { get; init; }
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
    internal static _3DProduct<CGTrader3DProductMetadata> WithCGTrader(this _3DProduct _3DProduct, _3DProduct.Metadata_ _)
    {
        var cgTraderMetadata = CGTrader3DProductMetadata.ForCG(
            _.Title,
            _.Description,
            _.Tags,
            Category(),
            License(),
            _.Price,
            _.GameReady,
            _.Animated,
            _.Rigged,
            _.PhysicallyBasedRendering,
            _.AdultContent,
            new(_.Polygons, _.Vertices, Geometry(), _.Collection, _.Textures, _.Materials, _.PluginsUsed, _.UVMapped, UnwrappedUVs())
            );
        return _3DProduct.With_(cgTraderMetadata);


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

        CGTrader._3DModelComponents.UnwrappedUVs_? UnwrappedUVs() => _.UnwrappedUVs switch
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

    internal static async Task<_3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>> AsyncWithTurboSquid(this _3DProduct _3DProduct, _3DProduct.Metadata_ _, INodeGui nodeGui, CancellationToken cancellationToken)
    {
        var tuboSquidMetadata = await TurboSquid3DProductMetadata.ProvideAsync(
            nodeGui,
            _.Title,
            _.Description,
            _.Tags,
            _.Polygons,
            _.Vertices,
            _.Price,
            License(),
            _.Animated,
            _.Collection,
            Geometry(),
            _.Materials,
            _.Rigged,
            _.Textures,
            _.UVMapped,
            UnwrappedUVs(),
            cancellationToken
        );
        return _3DProduct.With(nodeGui, tuboSquidMetadata);


        TurboSquid3DProductMetadata.License_ License() => _.License switch
        {
            _3DProduct.Metadata_.License_.RoyaltyFree => TurboSquid3DProductMetadata.License_.royalty_free_all_extended_uses,
            _3DProduct.Metadata_.License_.Editorial => TurboSquid3DProductMetadata.License_.royalty_free_editorial_uses_only,
            _ => throw new NotImplementedException()
        };

        TurboSquid3DProductMetadata.Geometry_? Geometry() => _.Geometry switch
        {
            _3DProduct.Metadata_.Geometry_.PolygonalQuadsOnly => TurboSquid3DProductMetadata.Geometry_.polygonal_quads_only,
            _3DProduct.Metadata_.Geometry_.PolygonalQuadsTris => TurboSquid3DProductMetadata.Geometry_.polygonal_quads_tris,
            _3DProduct.Metadata_.Geometry_.PolygonalTrisOnly => TurboSquid3DProductMetadata.Geometry_.polygonal_tris_only,
            _3DProduct.Metadata_.Geometry_.PolygonalNgonsUsed => TurboSquid3DProductMetadata.Geometry_.polygonal_ngons_used,
            _3DProduct.Metadata_.Geometry_.Polygonal => TurboSquid3DProductMetadata.Geometry_.polygonal,
            _3DProduct.Metadata_.Geometry_.Subdivision => TurboSquid3DProductMetadata.Geometry_.subdivision,
            _3DProduct.Metadata_.Geometry_.Nurbs => TurboSquid3DProductMetadata.Geometry_.nurbs,
            _3DProduct.Metadata_.Geometry_.Unknown => TurboSquid3DProductMetadata.Geometry_.unknown,
            null => null,
            _ => throw new NotImplementedException()
        };

        TurboSquid3DProductMetadata.UnwrappedUVs_? UnwrappedUVs() => _.UnwrappedUVs switch
        {
            _3DProduct.Metadata_.UnwrappedUVs_.NonOverlapping => TurboSquid3DProductMetadata.UnwrappedUVs_.yes_non_overlapping,
            _3DProduct.Metadata_.UnwrappedUVs_.Overlapping => TurboSquid3DProductMetadata.UnwrappedUVs_.yes_overlapping,
            _3DProduct.Metadata_.UnwrappedUVs_.Mixed => TurboSquid3DProductMetadata.UnwrappedUVs_.mixed,
            _3DProduct.Metadata_.UnwrappedUVs_.No => TurboSquid3DProductMetadata.UnwrappedUVs_.no,
            _3DProduct.Metadata_.UnwrappedUVs_.Unknown => TurboSquid3DProductMetadata.UnwrappedUVs_.unknown,
            null => null,
            _ => throw new NotImplementedException()
        };
    }

    static _3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> With(this _3DProduct _3DProduct, INodeGui nodeGui, TurboSquid3DProductMetadata metadata)
    {
        var turboSquid3DProduct = _3DProduct.With_(metadata);
        var turboSquidMetadataFile = TurboSquid3DProductMetadata.File.For(turboSquid3DProduct);
        if (!File.Exists(turboSquidMetadataFile.Path))
            turboSquidMetadataFile.Populate(nodeGui);
        return new(turboSquid3DProduct, turboSquidMetadataFile.Read());
    }

    public static _3DProduct<TMetadata> With_<TMetadata>(this _3DProduct _3DProduct, TMetadata metadata)
        => new(_3DProduct, metadata);
}
