using Autofac;
using Node.Plugins.Discoverers;

namespace Node.Plugins;

public static class PluginDiscoverers
{
    public static void RegisterDiscoverers(ContainerBuilder builder)
    {
        void register<T>() where T : IPluginDiscoverer =>
            builder.RegisterType<T>()
                .As<IPluginDiscoverer>()
                .SingleInstance();

        register<BlenderPluginDiscoverer>();
        register<Autodesk3dsMaxPluginDiscoverer>();
        register<TopazVideoAIPluginDiscoverer>();
        register<DaVinciResolvePluginDiscoverer>();
        register<UnityPluginDiscoverer>();
        register<FFmpegPluginDiscoverer>();
        register<PythonPluginDiscoverer>();
        register<PythonEsrganPluginDiscoverer>();
        register<StableDiffusionPluginDiscoverer>();
        register<Yolov7PluginDiscoverer>();
        register<ImageDetectorPluginDiscoverer>();
        register<RobustVideoMattingPluginDiscoverer>();
        register<OneClickPluginDiscoverer>();
        register<VeeeVectorizerPluginDiscoverer>();
        register<NvidiaDriverPluginDiscoverer>();
        register<DotnetRuntimePluginDiscoverer>();
        register<GitPluginDiscoverer>();
        register<CondaPluginDiscoverer>();
    }
}
