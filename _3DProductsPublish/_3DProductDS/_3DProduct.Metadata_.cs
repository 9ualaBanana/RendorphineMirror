using _3DProductsPublish.Turbosquid._3DModelComponents;
using Tomlyn;

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
        public bool Animated { get; init; } = false;
        public bool Collection { get; init; } = false;
        public bool Rigged { get; init; } = false;
        public bool Textures { get; init; } = false;
        public bool Materials { get; init; } = false;
        public bool UVMapped { get; init; } = false;
        public UnwrappedUVs_? UnwrappedUVs { get; init; } = default;
        public bool? GameReady { get; init; } = default;
        public bool? PhysicallyBasedRendering { get; init; } = default;
        public bool? AdultContent { get; init; } = default;
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
    internal static async Task<_3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>> AsyncWithTurboSquid(this _3DProduct _3DProduct, _3DProduct.Metadata_ _, CancellationToken cancellationToken)
    {
        var productMetadata = await TurboSquid3DProductMetadata.ProvideAsync(
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
        return _3DProduct.With(productMetadata).And(ModelsMetadata());


        IEnumerable<TurboSquid3DModelMetadata> ModelsMetadata()
        {
            var metadataFilePath = Path.Combine(_3DProduct.ContainerPath, TurboSquid3DProductMetadata.FileName);
            var modelsMetadata = Toml.Parse(File.ReadAllText(metadataFilePath))
                .Tables
                .Select(TurboSquid3DModelMetadata.Read);

            if (modelsMetadata.Count() == _3DProduct._3DModels.Count())
                if (modelsMetadata.Count(_ => _.IsNative) is 1)
                    return modelsMetadata;
                else throw new InvalidDataException($"Metadata file can mark only one {nameof(_3DModel)} as native.");
            else throw new InvalidDataException($"Metadata file doesn't describe every model of {nameof(_3DProduct)} ({metadataFilePath}).");
        }

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

    public static _3DProduct<TMetadata> With<TMetadata>(this _3DProduct _3DProduct, TMetadata metadata)
        => new(_3DProduct, metadata);

    internal static _3DProduct<TProductMetadata, TModelsMetadata> And<TProductMetadata, TModelsMetadata>(this _3DProduct<TProductMetadata> _3DProduct, IEnumerable<TModelsMetadata> modelsMetadata)
        where TModelsMetadata : I3DModelMetadata
        => new(_3DProduct, modelsMetadata);
}
