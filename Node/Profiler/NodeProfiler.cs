using Benchmark;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Node.Profiler;

internal class NodeProfiler
{
    readonly HttpClient _http;

    readonly static string _assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "assets");
    static string SampleVideoPath => Path.Combine(_assetsPath, "4k_sample_long.mp4");
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
            File.Create(Path.Combine(_assetsPath, $"{value}.version"));
        }
    }

    internal NodeProfiler(HttpClient httpClient)
    {
        _http = httpClient;
    }

    internal static async Task<BenchmarkResults?> RunBenchmarksAsyncIfBenchmarkVersionWasUpdated(uint testDataSize)
    {
        BenchmarkResults? hardwarePayload = null;

        var currentBenchmarkVersion = Assembly.GetAssembly(typeof(BenchmarkResult))!.GetName().Version;
        if (currentBenchmarkVersion != LastExecutedBenchmarkVersion)
        {
            hardwarePayload = await ComputeHardwarePayloadAsync(testDataSize);
            LastExecutedBenchmarkVersion = currentBenchmarkVersion;
        }
        return hardwarePayload;
    }

    static async Task<BenchmarkResults> ComputeHardwarePayloadAsync(uint testDataSize)
    {
        return new()
        {
            CPU = await ComputePayloadWithCPUBenchmarkResultsAsync(testDataSize),
            GPU = new()
            {
                Rating = default,
                FFmpegRating = default,
                Load = default,
            },
            RAM = GetRAMPayload(),
            Disks = await ComputePayloadWithDrivesBenchmarkResultsAsync(testDataSize)
        };
    }

    static async Task<CPUBenchmarkResults> ComputePayloadWithCPUBenchmarkResultsAsync(uint testDataSize)
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(SampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnCpuAsync()).Rate;
        }
        catch (Exception) { }
        return new()
        {
            Rating = (await new ZipBenchmark(testDataSize).RunAsync()).Rate,
            FFmpegRating = ffmpegRating,
            Load = default,
        };
    }

    static async Task<CPUBenchmarkResults> ComputePayloadWithGPUBenchmarkResultsAsync(uint testDataSize)
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(SampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnGpuAsync()).Rate;
        }
        catch (Exception) { }
        return new()
        {
            Rating = (await new ZipBenchmark(testDataSize).RunAsync()).Rate,
            FFmpegRating = ffmpegRating,
            Load = default,
        };
    }

    static async Task<DrivesBenchmarkResults[]> ComputePayloadWithDrivesBenchmarkResultsAsync(uint testDataSize)
    {
        var drivesBenchmarkResults = new List<(BenchmarkResult Read, BenchmarkResult Write)>();
        foreach (var logicalDiskName in Drive.LogicalDisksNamesFromDistinctDrives)
        {
            drivesBenchmarkResults.Add(await new ReadWriteBenchmark(testDataSize).RunAsync(logicalDiskName));
        }

        return Drive.Info.Zip(drivesBenchmarkResults)
            .Select(zip => new DrivesBenchmarkResults()
            {
                FreeSpace = zip.First.FreeSpace,
                WriteSpeed = zip.Second.Write.Rate
            })
            .ToArray();
    }

    static RAMBenchmarkResults GetRAMPayload()
    {
        var ramInfo = RAM.Info;
        return new()
        {
            Total = ramInfo.Aggregate(0ul, (totalCapacity, ramUnit) => totalCapacity += ramUnit.Capacity),
            Free = ramInfo.Aggregate(0ul, (freeMemory, ramUnit) => freeMemory += ramUnit.FreeMemory)
        };
    }

    internal async Task SendNodeProfileAsync(string serverUri, BenchmarkResults? benchmarkResults)
    {
        var requestPayload = await BuildPayloadAsync(benchmarkResults);

        try
        {
            var response = await _http.PostAsync(serverUri, requestPayload);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception) { }
    }

    static async Task<FormUrlEncodedContent> BuildPayloadAsync(BenchmarkResults? benchmarkResults)
    {
        var payloadContent = new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["info"] = await SerializeNodeProfileAsync(benchmarkResults),
        };
        return new FormUrlEncodedContent(payloadContent);
    }

    static async Task<string> SerializeNodeProfileAsync(BenchmarkResults? benchmarkResults)
    {
        return JsonSerializer.Serialize(new
        {
            ip = (await MachineInfo.GetPublicIPAsync()).ToString(),
            port = int.Parse(MachineInfo.Port),
            nickname = Settings.Username,
            allowedinputs = new { User = 1 },
            allowedoutputs = new { User = 1 },
            allowedtypes = new { },
            hardware = benchmarkResults,
        }, new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
    }
}
