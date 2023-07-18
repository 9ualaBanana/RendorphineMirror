using Node.Heartbeat;

namespace Node.Profiling;

public class MPlusHeartbeatGenerator : IHeartbeatGenerator
{
    public HttpRequestMessage Request => new HttpRequestMessage(HttpMethod.Post, $"{Api.TaskManagerEndpoint}/pheartbeat");
    public HttpContent Content => Profiler.GetAsync(PluginManager).GetAwaiter().GetResult();
    public EventHandler<HttpResponseMessage>? ResponseHandler { get; }

    readonly PluginManager PluginManager;

    public MPlusHeartbeatGenerator(PluginManager pluginManager) => PluginManager = pluginManager;
}
