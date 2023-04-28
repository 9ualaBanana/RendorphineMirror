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
