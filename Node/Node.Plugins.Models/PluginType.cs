namespace Node.Plugins.Models;

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
    RobustVideoMatting,

    NvidiaDriver,
    Conda,
}

// TODO:: delete this forever
public static class PluginTypeExtensions
{
    public static bool IsPlugin(this PluginType type) => !type.ToString().Contains('_');

    public static bool IsChildOf(this PluginType type, PluginType parent)
    {
        var typeName = type.ToString().ToLowerInvariant();
        var parentTypeName = parent.ToString().ToLowerInvariant();
        return typeName.StartsWith($"{parentTypeName}_") && typeName != parentTypeName;
    }
}
