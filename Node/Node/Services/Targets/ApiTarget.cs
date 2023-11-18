namespace Node.Services.Targets;

public class ApiTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterInstance(Api.Default.Client)
            .SingleInstance();

        builder.RegisterInstance(Api.Default)
            .SingleInstance();

        builder.RegisterType<NodeApis>()
            .As<Apis>()
            .SingleInstance();
    }

    public required ReconnectTarget Reconnect { get; init; }

    public async Task ExecuteAsync()
    {

    }
}
