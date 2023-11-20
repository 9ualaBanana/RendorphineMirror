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
        var result = await Send(Api, await Profiler.GetAsync());

        // upon receiving an error -259 from heartbeat, the node should switch to reconnect mode
        // i don't know how to do this easily, like pausing all tasks and stuff like that
        // so we just exit and let the pinger restart the node
        if (!result && result.Error is HttpError { ErrorCode: -259 })
            Environment.Exit(0);

        result.ThrowIfError();
    }

    public static async Task<OperationResult> Send(Api api, Profile profile)
    {
        var payloadContent = new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["info"] = JsonConvert.SerializeObject(profile, JsonSettings.Lowercase),
        };

        using var content = new FormUrlEncodedContent(payloadContent);
        return await api.ApiPost($"{Api.TaskManagerEndpoint}/pheartbeat", "Sending a heartbeat", content);
    }
}
