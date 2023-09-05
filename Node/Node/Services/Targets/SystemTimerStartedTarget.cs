namespace Node.Services.Targets;

public class SystemTimerStartedTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }
    public async Task ExecuteAsync() => SystemService.Start();
}
