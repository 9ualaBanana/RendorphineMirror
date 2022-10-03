using Benchmark;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Profiling;

internal static class Benchmark
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly static string _assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "assets");
    readonly static string _sampleVideoPath = Path.Combine(_assetsPath, "4k_sample.mp4");
    readonly static FileBackedVersion LatestExecutedVersion = new(_assetsPath);

    /// <summary>
    /// true if <see cref="Benchmark"/> wasn't run yet after it had been updated; false, otherwise.
    /// </summary>
    internal static bool ShouldBeRun => !isCompleted && IsUpdated;
    static bool isCompleted;
    /// <remarks>
    /// Evaluated once upon node start.
    /// </remarks>
    static bool IsUpdated => !LatestExecutedVersion.Exists || LatestExecutedVersion < BenchmarkMetadata.Version;

    internal static async Task<object> RunAsync(int testDataSize)
    {
        var result = await RunAsyncCore(testDataSize);

        isCompleted = true;
        LatestExecutedVersion.Update(BenchmarkMetadata.Version);

        _logger.Info("Benchmark completed:\n{BenchmarkResults}", JsonConvert.SerializeObject(result, Formatting.None));

        return result;
    }

    static async Task<object> RunAsyncCore(int testDataSize)
    {
        // todo remove when linux benchmark fixed
        if (Environment.OSVersion.Platform == PlatformID.Unix)
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

        var cpu = await GetCpuBenchmarkResultsAsObjectAsync(testDataSize);
        benchs["cpu"] = JToken.FromObject(cpu);
        var gpu = await GetGpuBenchmarkResultsAsObjectAsync();
        benchs["gpu"] = JToken.FromObject(gpu);
        var ram = GetRamAsObject();
        benchs["ram"] = JToken.FromObject(ram);
        var disks = await GetDrivesBenchmarkResultsAsObjectAsync(testDataSize);
        benchs["disks"] = JToken.FromObject(disks);

        return new
        {
            cpu,
            gpu,
            ram,
            disks,
        };
    }

    static async Task<object> GetCpuBenchmarkResultsAsObjectAsync(int testDataSize)
    {
        double ffmpegRating = default;
        uint load = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(_sampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnCpuAsync()).Bps;
            load = CPU.Info.First().LoadPercentage;
        }
        catch (Exception) { }
        return new
        {
            rating = (await new ZipBenchmark(testDataSize).RunAsync()).Bps,
            pratings = new { ffmpeg = ffmpegRating },
            load
        };
    }

    static async Task<object> GetGpuBenchmarkResultsAsObjectAsync()

    {
        double ffmpegRating = default;
        uint load = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(_sampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnGpuAsync()).Bps;
            load = GPU.Info.First().LoadPercentage;
        }
        catch (Exception) { }
        return new
        {
            rating = ffmpegRating,
            pratings = new { ffmpeg = ffmpegRating },
            load,
        };
    }

    static async Task<IEnumerable<object>> GetDrivesBenchmarkResultsAsObjectAsync(int testDataSize)
    {
        var drivesBenchmarkResults = new List<(BenchmarkResult Read, BenchmarkResult Write)>();
        var readWriteBenchmark = new ReadWriteBenchmark(testDataSize);

        foreach (var logicalDiskName in Drive.LogicalDisksNamesFromDistinctDrives)
            drivesBenchmarkResults.Add(await readWriteBenchmark.RunAsync(logicalDiskName));

        IEnumerable<object> result = Enumerable.Empty<object>();
        try 
        { 
            result = Drive.Info.Zip(drivesBenchmarkResults)
                .Select(zip => new
                {
                    freespace = zip.First.FreeSpace,
                    writespeed = zip.Second.Write.Bps
                });
        } catch { }
        return result;
    }

    static object GetRamAsObject()
    {
        var ramInfo = RAM.Info;

        ulong total = default;
        ulong free = default;
        try
        {
            total = ramInfo.Aggregate(0ul, (totalCapacity, ramUnit) => totalCapacity += ramUnit.Capacity);
            free = ramInfo.Aggregate(0ul, (freeMemory, ramUnit) => freeMemory += ramUnit.FreeMemory);
        }
        catch { }
        return new { total, free };
    }
}
