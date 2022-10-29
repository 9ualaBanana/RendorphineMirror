namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

public record AdditionalInfo(
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
