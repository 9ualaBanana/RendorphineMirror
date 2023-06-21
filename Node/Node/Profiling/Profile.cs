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

    public static async ValueTask<Profile> CreateDefault(PluginManager pluginManager)
    {
        var ip = MachineInfo.GetPublicIPAsync();
        var software = BuildSoftwarePayloadAsync(pluginManager);
        var types = BuildDefaultAllowedTypes(pluginManager);

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
            Hardware = Settings.BenchmarkResult.Value?.Data ?? throw new Exception("Could not create Profile without benchmark data"),
        };
    }

    public static async ValueTask<Dictionary<string, int>> BuildDefaultAllowedTypes(PluginManager pluginManager)
    {
        var installed = (await pluginManager.GetInstalledPluginsAsync())
            .Select(p => p.Type)
            .ToImmutableHashSet();

        return TaskList.AllActions
            .Where(a => !Settings.DisabledTaskTypes.Value.Contains(a.Name))
            .Where(a => a.RequiredPlugins.All(installed.Contains))
            .ToDictionary(a => a.Name.ToString(), _ => 1);
    }

    // Ridiculous.
    static async Task<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>> BuildSoftwarePayloadAsync(PluginManager pluginManager)
    {
        var result = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();
        foreach (var softwareGroup in (await pluginManager.GetInstalledPluginsAsync()).GroupBy(software => software.Type))
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
