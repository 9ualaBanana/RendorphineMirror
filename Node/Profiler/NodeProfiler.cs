﻿using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Benchmark;
using Timer = System.Timers.Timer;

namespace Node.Profiler;

/// <remarks>
/// Instances of the class are intended to be created once per use case.
/// </remarks>
internal class NodeProfiler
{
    public static object HeatbeatLock = new();

    readonly HttpClient _http;
    readonly Timer _intervalTimer;

    readonly static string _assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "assets");
    static string SampleVideoPath => Path.Combine(_assetsPath, "4k_sample.mp4");
    static Version? _lastExecutedBenchmarkVersion;
    static Version? LastExecutedBenchmarkVersion
    {
        get
        {
            if (_lastExecutedBenchmarkVersion is not null) return _lastExecutedBenchmarkVersion;

            var file = Directory.Exists(_assetsPath)
                ? Directory.EnumerateFiles(_assetsPath).SingleOrDefault(file => Path.GetExtension(file) == ".version")
                : null;

            if (file is null) return null;
            return _lastExecutedBenchmarkVersion = Version.Parse(Path.GetFileNameWithoutExtension(file));
        }
        set
        {
            Directory.CreateDirectory(_assetsPath);

            if (LastExecutedBenchmarkVersion is not null)
                File.Delete(Path.Combine(_assetsPath, $"{LastExecutedBenchmarkVersion}.version"));
            File.Create(Path.Combine(_assetsPath, $"{value}.version")).Dispose();
        }
    }

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

    internal static async Task<object?> RunBenchmarksAsyncIfBenchmarkVersionWasUpdated(int testDataSize)
    {
        object? hardwarePayload = null;

        var currentBenchmarkVersion = Assembly.GetAssembly(typeof(BenchmarkResult))!.GetName().Version;
        if (currentBenchmarkVersion != LastExecutedBenchmarkVersion)
        {
            hardwarePayload = await ComputeHardwarePayloadAsync(testDataSize);
            LastExecutedBenchmarkVersion = currentBenchmarkVersion;
        }
        return hardwarePayload;
    }

    static async Task<object> ComputeHardwarePayloadAsync(int testDataSize)
    {
        if (Environment.GetCommandLineArgs().Contains("release"))
        {
            return new
            {
                cpu = new
                {
                    rating = 10000000,
                    pratings = new { ffmpeg = 100 },
                    load = 0.0001,
                },
                gpu = new
                {
                    rating = 10000000,
                    pratings = new { ffmpeg = 100 },
                    load = 0.0001,
                },
                ram = new { total = 32678000000, free = 16678000000, },
                disks = new[] { new { freespace = 326780000000, writespeed = 32677000000 } },
            };
        }

        using var _ = new FuncDispose(NodeGlobalState.Instance.ExecutingBenchmarks.Clear);
        var benchs = NodeGlobalState.Instance.ExecutingBenchmarks;
        benchs.Execute(() => benchs["cpu"] = benchs["gpu"] = benchs["ram"] = benchs["disks"] = null);

        var cpu = await ComputePayloadWithCPUBenchmarkResultsAsync(testDataSize);
        benchs["cpu"] = Newtonsoft.Json.Linq.JObject.FromObject(cpu);
        var gpu = await ComputePayloadWithGPUBenchmarkResultsAsync();
        benchs["gpu"] = Newtonsoft.Json.Linq.JObject.FromObject(gpu);
        var ram = GetRAMPayload();
        benchs["ram"] = Newtonsoft.Json.Linq.JObject.FromObject(ram);
        var disks = await ComputePayloadWithDrivesBenchmarkResultsAsync(testDataSize);
        benchs["disks"] = Newtonsoft.Json.Linq.JObject.FromObject(disks);

        return new
        {
            cpu,
            gpu,
            ram,
            disks,
        };
    }

    static async Task<object> ComputePayloadWithCPUBenchmarkResultsAsync(int testDataSize)
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(SampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnCpuAsync()).Bps;
        }
        catch (Exception) { }
        using var zipBenchmark = new ZipBenchmark(testDataSize);
        return new
        {
            rating = (await zipBenchmark.RunAsync()).Bps,
            pratings = new { ffmpeg = ffmpegRating },
            load = -1,
        };
    }

    static async Task<object> ComputePayloadWithGPUBenchmarkResultsAsync()
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(SampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnGpuAsync()).Bps;
        }
        catch (Exception) { }
        return new
        {
            rating = -1,
            pratings = new { ffmpeg = ffmpegRating },
            load = -1,
        };
    }

    static async Task<IEnumerable<object>> ComputePayloadWithDrivesBenchmarkResultsAsync(int testDataSize)
    {
        var drivesBenchmarkResults = new List<(BenchmarkResult Read, BenchmarkResult Write)>();
        var readWriteBenchmark = new ReadWriteBenchmark(testDataSize);
        foreach (var logicalDiskName in Drive.LogicalDisksNamesFromDistinctDrives)
        {
            drivesBenchmarkResults.Add(await readWriteBenchmark.RunAsync(logicalDiskName));
        }

        return Drive.Info.Zip(drivesBenchmarkResults)
            .Select(zip => new
            {
                freespace = zip.First.FreeSpace,
                writespeed = zip.Second.Write.Bps
            });
    }

    static object GetRAMPayload()
    {
        var ramInfo = RAM.Info;
        return new
        {
            total = ramInfo.Aggregate(0ul, (totalCapacity, ramUnit) => totalCapacity += ramUnit.Capacity),
            free = ramInfo.Aggregate(0ul, (freeMemory, ramUnit) => freeMemory += ramUnit.FreeMemory)
        };
    }

    internal async Task SendNodeProfile(string serverUri, object? benchmarkResults, TimeSpan interval = default)
    {
        if (interval != default)
        {
            _intervalTimer.Interval = interval.TotalMilliseconds;
            _intervalTimer.Elapsed += (_, _) =>
            {
                lock (HeatbeatLock)
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
