using Node.Listeners;

namespace Node.Services.Targets;

public class ReleaseMainTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<AutoCleanup>()
            .SingleInstance();
    }

    public required BaseTarget Base { get; init; }
    public required ConnectedToMPlusTarget ConnectedToMPlus { get; init; }
    public required PublicListenersTarget PublicListeners { get; init; }
    public required TaskReceiverTarget ReadyToReceiveTasks { get; init; }
    public required AutoCleanup AutoCleanup { get; init; }
    public required DebugListener DebugListener { get; init; }

    public async Task ExecuteAsync()
    {
        AutoCleanup.Start();
    }
}
