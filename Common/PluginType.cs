namespace Common;

public enum PluginType
{
    FFmpeg,
    DaVinciResolve,
    TopazGigapixelAI,
    Autodesk3dsMax,
    Blender,
}

public static class PluginTypeExtensions
{
    public static string? GetName(this PluginType type) => type switch
    {
        PluginType.FFmpeg => "FFMpeg",
        PluginType.Blender => "Blender",
        PluginType.Autodesk3dsMax => "Autodesk 3ds Max",
        PluginType.TopazGigapixelAI => "Topaz Gigapixel AI",
        PluginType.DaVinciResolve => "Davinci Resolve",

        _ => type.ToString(),
    };
}
