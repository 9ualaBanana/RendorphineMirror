using _3DProductsPublish._3DProductDS;
using Tomlyn;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

public record TurboSquid3DProductMetadata
{
    internal const string FileName = "turbosquid.meta";

    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string[] Tags
    {
        get => _tags;
        init
        {
            if (value.Length < 1) throw new ArgumentOutOfRangeException(
                nameof(value.Length),
                value.Length,
                $"{nameof(TurboSquid3DProductMetadata)} requires at least 1 tag.");

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
    public bool Rigged { get; init; } = false;
    public bool Textures { get; init; } = false;
    public bool Materials { get; init; } = false;
    public bool UVMapped { get; init; } = false;
    public UnwrappedUVs_? UnwrappedUVs { get; init; } = default;


    public JObject ToProductForm(string draftId)
    {
        var productForm = JObject.FromObject(new
        {
            alpha_channel = false,
            animated = Animated,
            biped = false,
            certifications = Array.Empty<string>(),
            color_depth = 0,
            description = Description,
            display_tags = string.Join(' ', Tags),
            draft_id = draftId,
            frame_rate = 0,
            height = (string?)null,
            length = (string?)null,
            license = License.ToString(),
            loopable = false,
            materials = Materials,
            multiple_layers = false,
            name = Title,
            polygons = Polygons.ToString(),
            price = Price.ToString("0.00"),
            rigged = Rigged,
            status = "draft",
            textures = Textures,
            tileable = false,
            uv_mapped = UVMapped,
            vertices = Vertices.ToString(),
            width = (string?)null
        });
        productForm.Add("geometry", Geometry is not null ? new JValue(Geometry.ToString()) : new JValue(0));
        productForm.Add("unwrapped_u_vs", UnwrappedUVs is not null ? new JValue(UnwrappedUVs.ToString()) : new JValue(0));

        return productForm;
    }


    public enum License_
    {
        royalty_free_all_extended_uses,
        royalty_free_editorial_uses_only
    }

    public enum Geometry_
    {
        polygonal_quads_only,
        polygonal_quads_tris,
        polygonal_tris_only,
        polygonal_ngons_used,
        polygonal,
        subdivision,
        nurbs,
        unknown
    }

    public enum UnwrappedUVs_
    {
        yes_non_overlapping,
        yes_overlapping,
        mixed,
        no,
        unknown
    }
}
