namespace Common;

public enum PluginType
{
    FFmpeg,
    DaVinciResolve,
    TopazGigapixelAI,
    Autodesk3dsMax,
    Blender,
    Python,
}

public static class PluginTypeExtensions
{
    public static string? GetName(this PluginType type) => type switch
    {
        PluginType.Autodesk3dsMax => "Autodesk 3ds Max",
        PluginType.TopazGigapixelAI => "Topaz Gigapixel AI",
        PluginType.DaVinciResolve => "DaVinci Resolve",

        _ => type.ToString(),
    };
}
