using _3DProductsPublish._3DProductDS;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

public record TurboSquid3DProductMetadata : _3DModelMetadata
{
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
    public required int Price { get; init; }
    public required TurboSquidLicense License { get; init; }
    public bool Animated { get; } = false;
    public bool Rigged { get; } = false;
    public Geometry? Geometry { get; } = default;   // sent as 0 in JSON
    public required int Polygons
    {
        get => _polygons;
        init => _polygons = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Polygons), "Should be greater than 0");
    }
    int _polygons;
    public required int Vertices
    {
        get => _vertices;
        init => _vertices = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Vertices), "Should be greater than 0");
    }
    int _vertices;
    public bool Textures { get; } = false;
    public bool Materials { get; } = false;
    public bool UVMapped { get; } = false;
    public UnwrappedUVs? UnwrappedUVs { get; } = default;   // sent as 0 in JSON

    public JObject ToProductForm(string draftId)
        => JObject.FromObject(new
        {
            alpha_channel = false.ToString(),
            animated = Animated,
            biped = false.ToString(),
            certifications = Array.Empty<string>(),
            color_depth = 0,
            description = Description,
            display_tags = string.Join(' ', Tags),
            draft_id = draftId,
            frame_rate = 0,
            geometry = Geometry ?? 0,
            height = (string?)null,
            length = (string?)null,
            license = License.ToString(),
            loopable = false.ToString(),
            materials = Materials.ToString(),
            multiple_layers = false.ToString(),
            name = Title,
            polygons = Polygons,
            price = Price,
            rigged = Rigged.ToString(),
            status = "draft",
            textures = Textures.ToString(),
            tileable = false.ToString(),
            unwrapped_u_vs = UnwrappedUVs?.ToString() ?? "0",
            uv_mapped = UVMapped.ToString(),
            vertices = Vertices,
            width = (string?)null
        });
}

public enum TurboSquidLicense
{
    royalty_free_all_extended_uses,
    royalty_free_editorial_uses_only
}

public enum Geometry
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

public enum UnwrappedUVs
{
    yes_non_overlapping,
    yes_overlapping,
    mixed,
    no,
    unknown
}
