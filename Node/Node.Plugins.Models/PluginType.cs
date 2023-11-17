namespace Node.Plugins.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum PluginType
{
    FFmpeg,
    DaVinciResolve,
    TopazVideoAI,
    Autodesk3dsMax,
    Unity,
    Blender,
    VeeeVectorizer,

    Python,
    Esrgan,
    RobustVideoMatting,
    StableDiffusion,
    Yolov7,
    ImageDetector,
    OneClick,

    NvidiaDriver,
    Conda,
    DotnetRuntime,
    Git,
}
