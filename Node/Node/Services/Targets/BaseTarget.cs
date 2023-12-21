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

        builder.RegisterType<SystemLoadStoreService>()
            .SingleInstance();
    }

    public required ApiTarget Api { get; init; }
    public required DatabaseTarget Database { get; init; }
    public required TaskExecutorTarget TaskExecutor { get; init; }
    public required PluginUpdaterTarget PluginUpdater { get; init; }
    public required TaskListener TaskListener { get; init; }
    public required SystemLoadStoreService SystemLoadStoreService { get; init; }

    async Task IServiceTarget.ExecuteAsync()
    {
        SystemLoadStoreService.Start(default);
    }
}
