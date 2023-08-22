using Node.Heartbeat;

namespace Node.Services.Targets;

public class PluginUpdaterTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        var pluginManager = new PluginManager(PluginDiscoverers.GetAll());
        builder.RegisterInstance(pluginManager)
            .AsSelf()
            .As<IInstalledPluginsProvider>()
            .SingleInstance()
            .OnActivating(async m => await m.Instance.GetInstalledPluginsAsync());

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
