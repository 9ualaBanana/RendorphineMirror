namespace Node.Services.Targets;

public class SystemTimerStartedTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required Init Init { get; init; }

    public async Task ExecuteAsync() => SystemService.Start(Init.Configuration.UseAdminRights);
}
