namespace Common;

public enum PluginType
{
    FFmpeg,
    DaVinciResolve,
    TopazGigapixelAI,
    Autodesk3dsMax,
    Blender,

    Python,
    Python_Esrgan,
}

public static class PluginTypeExtensions
{
    public static string? GetName(this PluginType type) => type switch
    {
        PluginType.Autodesk3dsMax => "Autodesk 3ds Max",
        PluginType.TopazGigapixelAI => "Topaz Gigapixel AI",
        PluginType.DaVinciResolve => "DaVinci Resolve",

        PluginType.Python_Esrgan => "ESRGAN",

        _ => type.ToString(),
    };

    public static bool IsPlugin(this PluginType type) => !type.ToString().Contains('_');

    public static bool IsChildOf(this PluginType type, PluginType parent)
    {
        var typeName = type.ToString().ToLowerInvariant();
        var parentTypeName = parent.ToString().ToLowerInvariant();
        return typeName.StartsWith($"{parentTypeName}_") && typeName != parentTypeName;
    }
}
