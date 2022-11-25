namespace Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;

public record CGTrader3DModelAdditionalMetadata(
    int? Polygons = null,
    int? Vertices = null,
    GeometryType? GeometryType = null,  // sent as (null) in JSON
    bool? Collection = null,
    bool? Textures = false,
    bool Materials = true,
    bool PluginsUsed = false,
    bool UVWMapping = false,
    UnwrappedUVs? UnwrappedUVs = null)  // sent as (null) in JSON
{
}

public enum GeometryType
{
    polygonal_mesh,
    subdivision_ready,
    nurbs,
    other
}

public enum UnwrappedUVs
{
    unknown,
    non_overlapping,
    overlapping,
    mixed,
    no
}
