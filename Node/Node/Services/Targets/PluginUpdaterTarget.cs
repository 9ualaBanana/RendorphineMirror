using Node.Heartbeat;

namespace Node.Services.Targets;

public class PluginUpdaterTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        PluginDiscoverers.RegisterDiscoverers(builder);

        builder.RegisterType<PluginManager>()
            .AsSelf()
            .As<IInstalledPluginsProvider>()
            .SingleInstance();

        builder.RegisterType<SoftwareList>()
            .As<ISoftwareListProvider>()
            .SingleInstance();

        builder.RegisterType<PluginChecker>()
            .SingleInstance();

        builder.RegisterType<CondaManager>()
            .SingleInstance();

        builder.RegisterType<PluginDeployer>()
            .SingleInstance();

        builder.RegisterType<UserSettingsHeartbeat>()
            .SingleInstance();

        builder.Register(ctx => new PluginList(ctx.Resolve<PluginManager>().GetInstalledPluginsAsync().GetAwaiter().GetResult()))
            .SingleInstance();
    }

    public required UserSettingsHeartbeat UserSettingsHeartbeat { get; init; }

    public async Task ExecuteAsync()
    {
        UserSettingsHeartbeat.Start();
    }


    class SoftwareList : ISoftwareListProvider
    {
        public IReadOnlyDictionary<string, SoftwareDefinition> Software => NodeGlobalState.Instance.Software.Value;
    }
}
