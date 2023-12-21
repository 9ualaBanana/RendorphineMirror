using _3DProductsPublish;
using Node.Listeners;
using Node.Profiling;

namespace Node.Services.Targets;

public class BaseTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.Register(ctx => TorrentClientInstance.Instance = new TorrentClient(Settings.DhtPort, Settings.TorrentPort) { Logger = ctx.Resolve<ILogger<TorrentClient>>() })
            .AsSelf()
            .SingleInstance()
            .AutoActivate();

        builder.RegisterType<Profiler>()
            .SingleInstance();

        builder.RegisterType<NodeTaskRegistration>()
            .SingleInstance();

        builder.RegisterType<SessionManager>()
            .SingleInstance();
    }

    public required ApiTarget Api { get; init; }
    public required DatabaseTarget Database { get; init; }
    public required TaskExecutorTarget TaskExecutor { get; init; }
    public required PluginUpdaterTarget PluginUpdater { get; init; }
    public required TaskListener TaskListener { get; init; }
    public required _3DProductPublisherTarget _3DProductPublisher { get; init; }
}
