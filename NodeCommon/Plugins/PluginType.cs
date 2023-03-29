using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NodeCommon.Plugins;

[JsonConverter(typeof(StringEnumConverter))]
public enum PluginType
{
    FFmpeg,
    DaVinciResolve,
    TopazGigapixelAI,
    Autodesk3dsMax,
    Blender,
    VeeeVectorizer,

    Python,
    Esrgan,

    NvidiaDriver,
    Conda,
}

public static class PluginTypeExtensions
{
    public static string? GetName(this PluginType type) => type switch
    {
        PluginType.Autodesk3dsMax => "Autodesk 3ds Max",
        PluginType.TopazGigapixelAI => "Topaz Gigapixel AI",
        PluginType.DaVinciResolve => "DaVinci Resolve",

        PluginType.Esrgan => "ESRGAN",

        PluginType.NvidiaDriver => "Nvidia driver",

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
