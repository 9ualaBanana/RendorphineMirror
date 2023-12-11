using Node.Listeners;

namespace Node.Services.Targets;

public class UITarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<NodeStateSender>()
            .SingleInstance();
    }

    public required SettingsInstance Settings { get; init; }
    public required LocalListener LocalListener { get; init; }
    public required NodeGlobalStateInitializedTarget NodeGlobalStateInitializedTarget { get; init; }
    public required NodeStateListener NodeStateListener { get; init; }

    public required DataDirs Dirs { get; init; }

    async Task IServiceTarget.ExecuteAsync()
    {
        Settings.BLocalListenPort.Bindable.SubscribeChanged(() => File.WriteAllText(Path.Combine(Dirs.Data, "lport"), Settings.LocalListenPort.ToStringInvariant()), true);
    }
}
