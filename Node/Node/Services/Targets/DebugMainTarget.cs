using Node.Listeners;

namespace Node.Services.Targets;

public class DebugMainTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required BaseTarget Base { get; init; }
    public required DebugListener DebugListener { get; init; }

    public async Task ExecuteAsync()
    {

    }
}