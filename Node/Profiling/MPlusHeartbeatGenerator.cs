using Common.Heartbeat;

namespace Node.Profiling;

public class MPlusHeartbeatGenerator : IHeartbeatGenerator
{
    public HttpRequestMessage Request => new HttpRequestMessage(HttpMethod.Post, $"{Api.TaskManagerEndpoint}/pheartbeat") { Content = Profiler.RunAsync().GetAwaiter().GetResult() };
    public EventHandler<HttpResponseMessage>? ResponseHandler { get; }
}
