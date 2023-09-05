﻿using Benchmark;

namespace Node.Profiling;

public class Profiler
{
    bool HeartbeatLocked = false;
    Profile? _cachedProfile;

    public required IComponentContext ComponentContext { get; init; }
    public required PluginManager PluginManager { get; init; }
    public required ILogger<Profiler> Logger { get; init; }

    async ValueTask<Profile> CreateDefault()
    {
        var ip = MachineInfo.GetPublicIPAsync();
        var software = BuildSoftwarePayloadAsync();
        var types = BuildDefaultAllowedTypes();

        return new()
        {
            Ip = (await ip).ToString(),
            Software = await software,
            AllowedTypes = await types,
            AllowedInputs = ComponentContext.GetAllRegisteredKeys<TaskInputType>().ToDictionary(x => x, _ => 1),
            AllowedOutputs = ComponentContext.GetAllRegisteredKeys<TaskOutputType>().ToDictionary(x => x, _ => 1),
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

    async ValueTask<Dictionary<string, int>> BuildDefaultAllowedTypes()
    {
        var installed = (await PluginManager.GetInstalledPluginsAsync())
            .Select(p => p.Type)
            .ToImmutableHashSet();

        return ComponentContext.ResolveAllKeyed<IPluginActionInfo, TaskAction>()
            .Where(a => !Settings.DisabledTaskTypes.Value.Contains(a.Name))
            .Where(a => a.RequiredPlugins.All(installed.Contains))
            .ToDictionary(a => a.Name.ToString(), _ => 1);
    }

    // Ridiculous.
    async Task<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>> BuildSoftwarePayloadAsync()
    {
        var result = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();
        foreach (var softwareGroup in (await PluginManager.GetInstalledPluginsAsync()).GroupBy(software => software.Type))
        {
            var softwareName = Enum.GetName(softwareGroup.Key)!.ToLower();
            result.Add(softwareName, new Dictionary<string, Dictionary<string, Dictionary<string, string>>>());
            foreach (var version in softwareGroup)
            {
                result[softwareName].Add(version.Version.ToString(), new Dictionary<string, Dictionary<string, string>>());
                result[softwareName][version.Version.ToString()].Add("plugins", new Dictionary<string, string>());
            }
        }
        return result;
    }

    public FuncDispose LockHeartbeat()
    {
        HeartbeatLocked = true;
        return new FuncDispose(() => HeartbeatLocked = false);
    }

    internal async Task<HttpContent> GetAsync()
    {
        if (Benchmark.ShouldBeRun)
        {
            Logger.Info($"Benchmark version: {BenchmarkMetadata.Version}");
            await Benchmark.RunAsync(1 * 1024 * 1024 * 1024).ConfigureAwait(false);
        }

        while (HeartbeatLocked)
            await Task.Delay(100);

        return await BuildProfileAsync();
    }

    async Task<FormUrlEncodedContent> BuildProfileAsync()
    {
        _cachedProfile ??= await CreateDefault();
        Benchmark.UpdateValues(_cachedProfile.Hardware);

        if (Settings.AcceptTasks.Value)
        {
            if (_cachedProfile.AllowedTypes.Count == 0)
                _cachedProfile.AllowedTypes = await BuildDefaultAllowedTypes();
        }
        else
        {
            if (_cachedProfile.AllowedTypes.Count != 0)
                _cachedProfile.AllowedTypes.Clear();
        }

        var payloadContent = new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["info"] = JsonConvert.SerializeObject(_cachedProfile, JsonSettings.Lowercase),
        };
        return new FormUrlEncodedContent(payloadContent);
    }
}