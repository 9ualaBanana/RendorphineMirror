using Node.Heartbeat;

namespace Node.Profiling;

public class MPlusHeartbeatGenerator : IHeartbeatGenerator
{
    public HttpRequestMessage Request => new HttpRequestMessage(HttpMethod.Post, $"{Api.TaskManagerEndpoint}/pheartbeat");
    public HttpContent Content => Profiler.GetAsync(PluginManager).GetAwaiter().GetResult();
    public EventHandler<HttpResponseMessage>? ResponseHandler => async (_, response) =>
    {
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new JsonTextReader(new StreamReader(stream));
        var responseJson = JToken.Load(reader);
        var responseStatusCode = responseJson["ok"]?.Value<int>();
        if (responseStatusCode == 1) return;

        if (responseJson["errormessage"]?.Value<string>() is { } errmsg)
            throw new HttpRequestException(errmsg);

        var errcode = responseJson["errorcode"]?.Value<string>();

        // upon receiving an error -195 from heartbeat, the node should switch to reconnect mode
        // i don't know how to do this easily, like pausing all tasks and stuff like that
        // so we just exit and let the pinger restart the node
        if (errcode == "-195")
        {
            Environment.Exit(0);
            return;
        }

        if (errcode is not null)
            throw new HttpRequestException($"Heartbear responded with {errcode} error code");

        throw new HttpRequestException($"Heartbear responded with {responseStatusCode} status code");
    };

    readonly PluginManager PluginManager;

    public MPlusHeartbeatGenerator(PluginManager pluginManager) => PluginManager = pluginManager;
}
