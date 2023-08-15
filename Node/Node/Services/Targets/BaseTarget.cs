using Node.Listeners;
using Node.Profiling;

namespace Node.Services.Targets;

public class BaseTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<Profiler>()
            .SingleInstance();

        builder.RegisterType<NodeTaskRegistration>()
            .SingleInstance();
    }

    public required ApiTarget Api { get; init; }
    public required UITarget UI { get; init; }
    public required DatabaseTarget Database { get; init; }
    public required TaskExecutorTarget TaskExecutor { get; init; }
    public required TaskListener TaskListener { get; init; }

    public async Task ExecuteAsync()
    {

    }
}
