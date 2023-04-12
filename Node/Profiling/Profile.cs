using Node.Plugins;

namespace Node.Profiling;

internal class Profile
{
    public int Port => Settings.UPnpPort;
    public int WebPort => Settings.UPnpServerPort;
    public string Nickname => Settings.NodeName;
    public string Guid => Settings.Guid;
    public string Version => MachineInfo.Version;

#pragma warning disable CS8618 // Properties are not set
    public string Ip { get; set; }
    public Dictionary<TaskInputType, int> AllowedInputs { get; set; }
    public Dictionary<TaskOutputType, int> AllowedOutputs { get; set; }
    public Dictionary<string, int> AllowedTypes { get; set; }
    public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> Software { get; set; }
    public object Pricing { get; set; }
    public BenchmarkData Hardware { get; set; }
#pragma warning restore

    public static async ValueTask<Profile> CreateDefault()
    {
        var ip = MachineInfo.GetPublicIPAsync();
        var software = BuildSoftwarePayloadAsync();
        var types = BuildDefaultAllowedTypes();

        return new()
        {
            Ip = (await ip).ToString(),
            Software = await software,
            AllowedTypes = await types,
            AllowedInputs = TaskHandler.InputHandlerList.ToDictionary(x => x.Type, _ => 1),
            AllowedOutputs = TaskHandler.OutputHandlerList.ToDictionary(x => x.Type, _ => 1),
            Pricing = new
            {
                minunitprice = new
                {
                    ffmpeg = -1,
                },
                minbwprice = -1,
                minstorageprice = -1,
            },
            Hardware = NodeSettings.BenchmarkResult.Value?.Data ?? throw new Exception("Could not create Profile without benchmark data"),
        };
    }

    public static async ValueTask<Dictionary<string, int>> BuildDefaultAllowedTypes()
    {
        var plugins = await MachineInfo.DiscoverInstalledPluginsInBackground();
        return plugins
            .SelectMany(x => TaskList.Get(x.Type))
            .DistinctBy(x => x.Name)
            .Where(x => !Settings.DisabledTaskTypes.Value.Contains(x.Name))
            .ToDictionary(x => x.Name.ToString(), _ => 1);
    }

    // Ridiculous.
    static async Task<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>> BuildSoftwarePayloadAsync()
    {
        var result = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();
        foreach (var softwareGroup in (await PluginsManager.DiscoverInstalledPluginsAsync()).GroupBy(software => software.Type))
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
