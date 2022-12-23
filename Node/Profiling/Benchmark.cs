using Benchmark;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Profiling;

internal static class Benchmark
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly static string _assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "assets");
    readonly static string _sampleVideoPath = Path.Combine(_assetsPath, "4k_sample.mp4");

    /// <summary>
    /// true if <see cref="Benchmark"/> wasn't run yet after it had been updated; false, otherwise.
    /// </summary>
    internal static bool ShouldBeRun => !isCompleted && IsUpdated;
    static bool isCompleted;
    /// <remarks>
    /// Evaluated once upon node start.
    /// </remarks>
    static bool IsUpdated
    {
        get
        {
            try { return NodeSettings.BenchmarkResult.Value?.Version?.Equals(BenchmarkMetadata.Version) != true; }
            catch
            {
                NodeSettings.BenchmarkResult.Delete();
                return true;
            }
        }
    }

    internal static async Task<BenchmarkData> RunAsync(int testDataSize)
    {
        var result = await RunAsyncCore(testDataSize);
        NodeSettings.BenchmarkResult.Value = new(BenchmarkMetadata.Version, result);
        UpdateValues(result);

        isCompleted = true;
        _logger.Info("Benchmark completed:\n{BenchmarkResults}", JsonConvert.SerializeObject(result, Formatting.None));

        return result;
    }
    internal static void UpdateValues(BenchmarkData result)
    {
        // need to update these values every hearbeat
        // later benchmark will be decoupled from this

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            result.CPU.Load = CPU.Info.First().LoadPercentage / 100d;
            result.GPU.Load = GPU.Info.First().LoadPercentage / 100d;
            result.RAM.Free = RAM.Info.Aggregate(0ul, (freeMemory, ramUnit) => freeMemory += ramUnit.FreeMemory);

            var drives = Drive.Info;
            foreach (var disk in result.Disks)
                disk.FreeSpace = drives.Find(x => x.Id == disk.Id)?.FreeSpace ?? disk.FreeSpace;
        }

        _logger.Trace($"Updated hardware values: cpu load {result.CPU.Load}; gpu load {result.GPU.Load}; ram free {result.RAM.Free}; disks free {string.Join(", ", result.Disks.Select(x => x.FreeSpace))};");
    }

    static async Task<BenchmarkData> RunAsyncCore(int testDataSize)
    {
        // todo remove when linux benchmark fixed
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            return new(
                new(10000000, new(100)) { Load = 0.0001 },
                new(10000000, new(100)) { Load = 0.0001 },
                new(32678000000) { Free = 16678000000 },
                new[] { new DriveBenchmarkResult(0, 32677000000) { FreeSpace = 326780000000 } }.ToList()
            );
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

        return new(
            cpu,
            gpu,
            ram,
            disks
        );
    }

    static async Task<CPUBenchmarkResult> GetCpuBenchmarkResultsAsObjectAsync(int testDataSize)
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(_sampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnCpuAsync()).Bps;
        }
        catch (Exception) { }
        return new((await new ZipBenchmark(testDataSize).RunAsync()).Bps, new(ffmpegRating));
    }

    static async Task<GPUBenchmarkResult> GetGpuBenchmarkResultsAsObjectAsync()
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(_sampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnGpuAsync()).Bps;
        }
        catch (Exception) { }
        return new GPUBenchmarkResult(ffmpegRating, new(ffmpegRating));
    }

    static async Task<List<DriveBenchmarkResult>> GetDrivesBenchmarkResultsAsObjectAsync(int testDataSize)
    {
        var drivesBenchmarkResults = new List<(BenchmarkResult Read, BenchmarkResult Write)>();
        var readWriteBenchmark = new ReadWriteBenchmark(testDataSize);

        foreach (var logicalDiskName in Drive.LogicalDisksNamesFromDistinctDrives)
            drivesBenchmarkResults.Add(await readWriteBenchmark.RunAsync(logicalDiskName));

        IEnumerable<DriveBenchmarkResult>? result = null;
        try
        {
            result = Drive.Info.Zip(drivesBenchmarkResults)
                .Select(zip => new DriveBenchmarkResult(zip.First.Id, zip.Second.Write.Bps) { FreeSpace = zip.First.FreeSpace });
        }
        catch { }
        return result?.ToList() ?? new();
    }

    static RAMInfo GetRamAsObject()
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
        return new(total) { Free = free };
    }
}
