using Node.Listeners;

namespace Node.Services.Targets;

public class UITarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required LocalListener LocalListener { get; init; }
    public required NodeStateListener NodeStateListener { get; init; }
    public required NodeGlobalStateInitializedTarget NodeGlobalStateInitializedTarget { get; init; }

    public async Task ExecuteAsync()
    {

    }
}
