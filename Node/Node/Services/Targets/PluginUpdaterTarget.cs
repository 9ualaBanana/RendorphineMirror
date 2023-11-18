using Node.Heartbeat;

namespace Node.Services.Targets;

public class PluginUpdaterTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        PluginDiscoverers.RegisterDiscoverers(builder);

        builder.RegisterInstance(new PluginDirs("plugins"))
            .SingleInstance();

        builder.RegisterType<PluginManager>()
            .AsSelf()
            .As<IPluginList>()
            .SingleInstance();

        builder.RegisterType<CondaManager>()
            .SingleInstance();

        builder.RegisterType<PluginDeployer>()
            .SingleInstance();

        builder.RegisterType<UserSettingsHeartbeat>()
            .SingleInstance();
    }

    public required UserSettingsHeartbeat UserSettingsHeartbeat { get; init; }

    public async Task ExecuteAsync()
    {
        UserSettingsHeartbeat.Start();
    }
}
