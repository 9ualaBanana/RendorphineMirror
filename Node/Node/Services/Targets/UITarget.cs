using Node.Listeners;

namespace Node.Services.Targets;

public class UITarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<NodeStateSender>()
            .SingleInstance();
    }

    public required LocalListener LocalListener { get; init; }
    public required NodeGlobalStateInitializedTarget NodeGlobalStateInitializedTarget { get; init; }
    public required NodeStateListener NodeStateListener { get; init; }

    public required DataDirs Dirs { get; init; }

    public async Task ExecuteAsync()
    {
        Settings.BLocalListenPort.Bindable.SubscribeChanged(() => File.WriteAllText(Path.Combine(Dirs.Data, "lport"), Settings.LocalListenPort.ToString()), true);
    }
}
