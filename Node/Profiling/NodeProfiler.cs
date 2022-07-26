using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Timer = System.Timers.Timer;

namespace Node.Profiling;

/// <remarks>
/// Instances of the class are intended to be created once per use case.
/// </remarks>
internal class NodeProfiler
{
    public static object HeartbeatLock = new();

    readonly HttpClient _http;
    readonly Timer _intervalTimer;

    static bool _nodeSettingsChanged;
    static FormUrlEncodedContent? _payload;

    static NodeProfiler()
    {
        Settings.AnyChanged += () => _nodeSettingsChanged = true;
    }

    internal NodeProfiler(HttpClient httpClient)
    {
        _http = httpClient;
        _intervalTimer = new();
    }

    internal async Task SendNodeProfile(string serverUri, object? benchmarkResults, TimeSpan interval = default)
    {
        if (interval != default)
        {
            _intervalTimer.Interval = interval.TotalMilliseconds;
            _intervalTimer.Elapsed += (_, _) =>
            {
                lock (HeartbeatLock)
                    MakePostRequest(serverUri, benchmarkResults).ConfigureAwait(false).GetAwaiter().GetResult();
            };
            _intervalTimer.AutoReset = true;
        }
        await MakePostRequest(serverUri, benchmarkResults);
        _intervalTimer.Start();
    }

    async Task MakePostRequest(string serverUri, object? benchmarkResults)
    {
        try
        {
            var response = await _http.PostAsync(serverUri, await GetPayloadAsync(benchmarkResults));
            response.EnsureSuccessStatusCode();
        }
        catch (Exception) { }
    }

    internal static async Task<FormUrlEncodedContent> GetPayloadAsync(object? benchmarkResults)
    {
        if (_payload is not null && !_nodeSettingsChanged) return _payload;

        var payloadContent = new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["info"] = await SerializeNodeProfileAsync(benchmarkResults),
        };
        return _payload = new FormUrlEncodedContent(payloadContent);
    }

    static async Task<string> SerializeNodeProfileAsync(object? benchmarkResults)
    {
        var allowedtypes = new JsonObject();
        foreach (var plugin in await MachineInfo.DiscoverInstalledPluginsInBackground().ConfigureAwait(false))
            foreach (var action in TaskList.Get(plugin.Type))
                allowedtypes[action.Name] = 1;

        var obj = new JsonObject()
        {
            ["ip"] = (await MachineInfo.GetPublicIPAsync()).ToString(),
            ["port"] = int.Parse(MachineInfo.Port),
            ["nickname"] = Settings.NodeName,
            ["guid"] = Settings.Guid,
            ["version"] = MachineInfo.Version,
            ["allowedinputs"] = new JsonObject()
            {
                [TaskInputOutputType.MPlus.ToString()] = 1
            },
            ["allowedoutputs"] = new JsonObject()
            {
                [TaskInputOutputType.MPlus.ToString()] = 1
            },
            ["allowedtypes"] = allowedtypes,
            ["pricing"] = new JsonObject()
            {
                ["minunitprice"] = new JsonObject()
                {
                    ["ffmpeg"] = -1,
                },
                ["minbwprice"] = -1,
                ["minstorageprice"] = -1,
            },
            ["software"] = JsonSerializer.SerializeToNode(await BuildSoftwarePayloadAsync()),
        };

        if (benchmarkResults is not null)
            obj["hardware"] = JsonSerializer.SerializeToNode(benchmarkResults);

        return obj.ToJsonString(new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
    }

    // Ridiculous.
    static async Task<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>> BuildSoftwarePayloadAsync()
    {
        var result = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();
        foreach (var softwareGroup in (await MachineInfo.DiscoverInstalledPluginsInBackground()).GroupBy(software => software.Type))
        {
            var softwareName = Enum.GetName(softwareGroup.Key)!.ToLower();
            result.Add(softwareName, new Dictionary<string, Dictionary<string, Dictionary<string, string>>>());
            foreach (var version in softwareGroup)
            {
                result[softwareName].Add(version.Version, new Dictionary<string, Dictionary<string, string>>());
                result[softwareName][version.Version].Add("plugins", new Dictionary<string, string>());
            }
        }
        return result;
    }
}
