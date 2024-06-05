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
    public required TaskExecutorTarget TaskExecutor { get; init; }
    public required AutoCleanup AutoCleanup { get; init; }

    async Task IServiceTarget.ExecuteAsync()
    {
        AutoCleanup.Start();
    }
}
