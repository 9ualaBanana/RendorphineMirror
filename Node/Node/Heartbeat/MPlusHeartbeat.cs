using Node.Profiling;

namespace Node.Heartbeat;

public class MPlusHeartbeat : Heartbeat
{
    protected override TimeSpan Interval { get; } = TimeSpan.FromMinutes(1);

    readonly Api Api;
    readonly PluginManager PluginManager;
    readonly Profiler Profiler;

    public MPlusHeartbeat(Api api, PluginManager pluginManager, Profiler profiler, ILogger<MPlusHeartbeat> logger) : base(logger)
    {
        Api = api;
        PluginManager = pluginManager;
        Profiler = profiler;
    }

    protected override async Task Execute()
    {
        var content = await Profiler.GetAsync(PluginManager);
        var result = await Api.ApiPost($"{Api.TaskManagerEndpoint}/pheartbeat", "Sending a heartbeat", content);

        // upon receiving an error -195 from heartbeat, the node should switch to reconnect mode
        // i don't know how to do this easily, like pausing all tasks and stuff like that
        // so we just exit and let the pinger restart the node
        if (!result && result.HttpData?.ErrorCode == -195)
            Environment.Exit(0);

        result.ThrowIfError();
    }
}
