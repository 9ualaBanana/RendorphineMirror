namespace Node.Services.Targets;

public class SystemTimerStartedTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required Init Init { get; init; }

    async Task IServiceTarget.ExecuteAsync() => SystemService.Start(Init.Configuration.UseAdminRights);
}
