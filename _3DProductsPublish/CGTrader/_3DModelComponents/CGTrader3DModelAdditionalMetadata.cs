namespace _3DProductsPublish.CGTrader._3DModelComponents;

public record CGTrader3DModelAdditionalMetadata(
    int? Polygons = null,
    int? Vertices = null,
    Geometry_? Geometry = null,  // sent as (null) in JSON
    bool? Collection = null,
    bool? Textures = false,
    bool Materials = true,
    bool PluginsUsed = false,
    bool UVWMapping = false,
    UnwrappedUVs_? UnwrappedUVs = null)  // sent as (null) in JSON
{
}

public enum Geometry_
{
    polygonal_mesh,
    subdivision_ready,
    nurbs,
    other
}

public enum UnwrappedUVs_
{
    unknown,
    non_overlapping,
    overlapping,
    mixed,
    no
}
