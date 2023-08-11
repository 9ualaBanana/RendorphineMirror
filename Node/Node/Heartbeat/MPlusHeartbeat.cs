using Node.Profiling;

namespace Node.Heartbeat;

public class MPlusHeartbeat : Heartbeat
{
    protected override TimeSpan Interval { get; } = TimeSpan.FromMinutes(1);

    public required Api Api { get; init; }
    public required Profiler Profiler { get; init; }

    public MPlusHeartbeat(ILogger<MPlusHeartbeat> logger) : base(logger) { }

    protected override async Task Execute()
    {
        var content = await Profiler.GetAsync();
        var result = await Api.ApiPost($"{Api.TaskManagerEndpoint}/pheartbeat", "Sending a heartbeat", content);

        // upon receiving an error -195 from heartbeat, the node should switch to reconnect mode
        // i don't know how to do this easily, like pausing all tasks and stuff like that
        // so we just exit and let the pinger restart the node
        if (!result && result.HttpData?.ErrorCode == -195)
            Environment.Exit(0);

        result.ThrowIfError();
    }
}
