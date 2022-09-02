using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Node.Profiling;

internal static class Profiler
{
    public static object HeartbeatLock = new();

    static FormUrlEncodedContent _cachedProfile = null!;
    static object? _benchmarkResults;
    static bool _settingsChanged = true;


    static Profiler()
    {
        Settings.AnyChanged += () => _settingsChanged = true;
    }

    internal static async Task<HttpContent> RunAsync()
    {
        if (Benchmark.ShouldBeRun)
            _benchmarkResults = await Benchmark.RunAsync(1073741824/*1GB*/).ConfigureAwait(false);

        //if (!_settingsChanged) return _cachedProfile;

        lock (HeartbeatLock)
            return BuildProfileAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    static async Task<FormUrlEncodedContent> BuildProfileAsync()
    {
        var payloadContent = new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["info"] = await SerializeNodeProfileAsync().ConfigureAwait(false),
        };
        return _cachedProfile = new FormUrlEncodedContent(payloadContent);
    }

    static async Task<string> SerializeNodeProfileAsync()   
    {
        var allowedtypes = new JsonObject();
        foreach (var plugin in await MachineInfo.DiscoverInstalledPluginsInBackground().ConfigureAwait(false))
            foreach (var action in TaskList.Get(plugin.Type))
                allowedtypes[action.Name] = 1;

        var obj = new JsonObject()
        {
            ["ip"] = (await MachineInfo.GetPublicIPAsync()).ToString(),
            ["port"] = int.Parse(MachineInfo.Port),
            ["webport"] = int.Parse(MachineInfo.WebServerPort),
            ["nickname"] = Settings.NodeName,
            ["guid"] = Settings.Guid,
            ["version"] = MachineInfo.Version,
            ["allowedinputs"] = new JsonObject()
            {
                [TaskInputType.MPlus.ToString()] = 1,
                [TaskInputType.DownloadLink.ToString()] = 1,
            },
            ["allowedoutputs"] = new JsonObject()
            {
                [TaskOutputType.MPlus.ToString()] = 1
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

        if (_benchmarkResults is not null)
            obj["hardware"] = JsonSerializer.SerializeToNode(_benchmarkResults);

        return obj.ToJsonString(new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
    }

    // Ridiculous.
    static async Task<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>> BuildSoftwarePayloadAsync()
    {
        var result = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();
        foreach (var softwareGroup in (await PluginsManager.DiscoverInstalledPluginsInBackground()).GroupBy(software => software.Type))
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
