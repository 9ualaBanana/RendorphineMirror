using System.Reflection;
using System.Text.Json;
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

            var file = Directory.EnumerateFiles(_assetsPath)
                .SingleOrDefault(file => Path.GetExtension(file) == ".version");

            if (file is null) return null;
            return _lastExecutedBenchmarkVersion = Version.Parse(Path.GetFileNameWithoutExtension(file));
        }
        set
        {
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

    internal static async Task<object?> RunBenchmarksAsyncIfBenchmarkVersionWasUpdated(uint testDataSize)
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

    static async Task<object> ComputeHardwarePayloadAsync(uint testDataSize)
    {
        var state = new BenchmarkNodeState();
        using var _ = GlobalState.SetState(state);

        var cpu = await ComputePayloadWithCPUBenchmarkResultsAsync(testDataSize);
        state.Completed.Add("cpu");
        var gpu = await ComputePayloadWithGPUBenchmarkResultsAsync();
        state.Completed.Add("gpu");
        var ram = GetRAMPayload();
        state.Completed.Add("ram");
        var disks = await ComputePayloadWithDrivesBenchmarkResultsAsync(testDataSize);
        state.Completed.Add("disks");

        return new
        {
            cpu,
            gpu,
            ram,
            disks,
        };
    }

    static async Task<object> ComputePayloadWithCPUBenchmarkResultsAsync(uint testDataSize)
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(SampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnCpuAsync()).Rate;
        }
        catch (Exception) { }
        using var zipBenchmark = new ZipBenchmark(testDataSize);
        return new
        {
            rating = (await zipBenchmark.RunAsync()).Rate,
            pratings = new { ffmpeg = ffmpegRating },
            load = -1,
        };
    }

    static async Task<object> ComputePayloadWithGPUBenchmarkResultsAsync()
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(SampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnGpuAsync()).Rate;
        }
        catch (Exception) { }
        return new
        {
            rating = -1,
            pratings = new { ffmpeg = ffmpegRating },
            load = -1,
        };
    }

    static async Task<IEnumerable<object>> ComputePayloadWithDrivesBenchmarkResultsAsync(uint testDataSize)
    {
        var drivesBenchmarkResults = new List<(BenchmarkResult Read, BenchmarkResult Write)>();
        var readWriteBenchmark = new ReadWriteBenchmark(testDataSize);
        {
            foreach (var logicalDiskName in Drive.LogicalDisksNamesFromDistinctDrives)
            {
                drivesBenchmarkResults.Add(await readWriteBenchmark.RunAsync(logicalDiskName));
            }
        }

        return Drive.Info.Zip(drivesBenchmarkResults)
            .Select(zip => new
            {
                freespace = zip.First.FreeSpace,
                writespeed = zip.Second.Write.Rate
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

    internal void SendNodeProfileAsync(string serverUri, object? benchmarkResults, TimeSpan interval = default)
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
        _ = MakePostRequest(serverUri, benchmarkResults);
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
        return JsonSerializer.Serialize(new
        {
            ip = (await MachineInfo.GetPublicIPAsync()).ToString(),
            port = int.Parse(MachineInfo.Port),
            nickname = Settings.NodeName,
            version = MachineInfo.Version,
            allowedinputs = new { User = 1 },
            allowedoutputs = new { User = 1 },
            allowedtypes = new { },
            pricing = new
            {
                minunitprice = new { ffmpeg = -1 },
                minbwprice = -1,
                minstorageprice = -1
            },
            hardware = benchmarkResults,
            software = await BuildSoftwarePayloadAsync()
        }, new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
    }

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
