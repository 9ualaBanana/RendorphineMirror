using Node.Plugins.Discoverers;

namespace Node.Plugins;

public static class PluginDiscoverers
{
    public static IPluginDiscoverer[] GetAll() =>
        new IPluginDiscoverer[]
        {
            new BlenderPluginDiscoverer(),
            new Autodesk3dsMaxPluginDiscoverer(),
            new TopazVideoAIPluginDiscoverer(),
            new DaVinciResolvePluginDiscoverer(),
            new FFmpegPluginDiscoverer(),
            new PythonPluginDiscoverer(),
            new PythonEsrganPluginDiscoverer(),
            new StableDiffusionPluginDiscoverer(),
            new Yolov7PluginDiscoverer(),
            new ImageDetectorPluginDiscoverer(),
            new RobustVideoMattingPluginDiscoverer(),
            new VeeeVectorizerPluginDiscoverer(),
            new NvidiaDriverPluginDiscoverer(),
            new DotnetRuntimePluginDiscoverer(),
            new GitPluginDiscoverer(),
            new CondaPluginDiscoverer()
        };
}
