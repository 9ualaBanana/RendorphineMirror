namespace Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;

public record CGTrader3DModelAdditionalMetadata(
    int? Polygons = null,
    int? Vertices = null,
    GeometryType? GeometryType = null,
    bool? Collection = null,
    bool? Textures = null,
    bool? Materials = null,
    bool? PluginsUsed = null,
    bool? UVWMapping = null,
    UnwrappedUVs? UnwrappedUVs = null)
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
