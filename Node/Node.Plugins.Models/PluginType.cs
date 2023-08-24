namespace Node.Plugins.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum PluginType
{
    FFmpeg,
    DaVinciResolve,
    TopazVideoAI,
    Autodesk3dsMax,
    Blender,
    VeeeVectorizer,

    Python,
    Esrgan,
    RobustVideoMatting,
    StableDiffusion,
    Yolov7,

    NvidiaDriver,
    Conda,
    DotnetRuntime,
    Git,
}