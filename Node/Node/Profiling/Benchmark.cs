using Benchmark;

namespace Node.Profiling;

[AutoRegisteredService(true)]
public class Benchmark
{
    public required NodeDataDirs DataDirs { get; init; }
    public required ILogger<Benchmark> Logger { get; init; }

    readonly string _assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "assets");
    string _sampleVideoPath => Path.Combine(_assetsPath, "4k_sample.mp4");

    /// <summary>
    /// true if <see cref="Benchmark"/> wasn't run yet after it had been updated; false, otherwise.
    /// </summary>
    internal bool ShouldBeRun => !isCompleted && IsUpdated;
    bool isCompleted;
    /// <remarks>
    /// Evaluated once upon node start.
    /// </remarks>
    bool IsUpdated
    {
        get
        {
            try { return Settings.BenchmarkResult.Value?.Version?.Equals(BenchmarkMetadata.Version) != true; }
            catch
            {
                Settings.BenchmarkResult.Delete();
                return true;
            }
        }
    }

    internal async Task<BenchmarkData> RunAsync(int testDataSize)
    {
        var result = await RunAsyncCore(testDataSize);
        Settings.BenchmarkResult.Value = new(BenchmarkMetadata.Version, result);
        UpdateValues(result);

        isCompleted = true;
        Logger.LogInformation("Benchmark completed:\n{BenchmarkResults}", JsonConvert.SerializeObject(result, Formatting.None));

        return result;
    }
    internal void UpdateValues(BenchmarkData result)
    {
        // need to update these values every hearbeat
        // later benchmark will be decoupled from this

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            result.CPU.Load = CPU.Info.Select(i => i.LoadPercentage / 100d).Average();
            result.GPU.Load = GPU.Info.Select(i => i.LoadPercentage / 100d).Average();
            result.RAM.Free = RAM.Info.Aggregate(0ul, (freeMemory, ramUnit) => freeMemory += ramUnit.FreeMemory);

            var drives = new MultiList<Drive>()
            {
                Drive.Info
                    .Where(d => d.LogicalDrives.Any(d => Path.GetFullPath(DataDirs.TaskDataDirectory()).StartsWith(d.RootDirectory.FullName, StringComparison.Ordinal)))
                    .MaxBy(d => d.LogicalDrives.First().RootDirectory.FullName.Length),
            };

            foreach (var disk in result.Disks)
                disk.FreeSpace = drives.FirstOrDefault(x => x.Id == disk.Id)?.FreeSpace ?? disk.FreeSpace;
        }
        if (OperatingSystem.IsLinux())
        {
            result.CPU.Load = CPU.Info.Select(i => i.LoadPercentage / 100d).Average();
            // result.GPU.Load = GPU.Info.Select(i => i.LoadPercentage / 100d).Average();
            result.RAM.Free = RAM.Info.Aggregate(0ul, (freeMemory, ramUnit) => freeMemory += ramUnit.FreeMemory);
        }

        Logger.Trace($"Updated hardware values: cpu load {result.CPU.Load}; gpu load {result.GPU.Load}; ram free {result.RAM.Free}; disks free {string.Join(", ", result.Disks.Select(x => x.FreeSpace))};");
    }

    async Task<BenchmarkData> RunAsyncCore(int testDataSize)
    {
        // todo remove when linux benchmark fixed
        if (!OperatingSystem.IsWindows())
        {
            return new(
                new(10000000, new(100)) { Load = 0.0001 },
                new(10000000, new(100)) { Load = 0.0001 },
                new(32678000000) { Free = 16678000000 },
                new[] { new DriveBenchmarkResult(0, 32677000000) { FreeSpace = 326780000000 } }.ToList()
            );
        }


        using var _logscope = Logger.BeginScope("Benchmakr");
        using var _ = new FuncDispose(NodeGlobalState.Instance.ExecutingBenchmarks.Clear);
        var benchs = NodeGlobalState.Instance.ExecutingBenchmarks;
        benchs.Execute(() => benchs["cpu"] = benchs["gpu"] = benchs["ram"] = benchs["disks"] = null);

        Logger.Info("Starting gpu");
        var cpu = await GetCpuBenchmarkResultsAsObjectAsync(testDataSize);
        benchs["cpu"] = JToken.FromObject(cpu);
        Logger.Info("Starting ram");
        var gpu = await GetGpuBenchmarkResultsAsObjectAsync();
        benchs["gpu"] = JToken.FromObject(gpu);
        Logger.Info("Starting cpu");
        var ram = GetRamAsObject();
        benchs["ram"] = JToken.FromObject(ram);
        Logger.Info("Starting disks");
        var disks = await GetDrivesBenchmarkResultsAsObjectAsync(testDataSize);
        benchs["disks"] = JToken.FromObject(disks);
        Logger.Info("Completed");

        return new(
            cpu,
            gpu,
            ram,
            disks
        );
    }

    async Task<CPUBenchmarkResult> GetCpuBenchmarkResultsAsObjectAsync(int testDataSize)
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(_sampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnCpuAsync()).Bps;
        }
        catch (Exception) { }
        return new((await new ZipBenchmark(testDataSize).RunAsync()).Bps, new(ffmpegRating));
    }

    async Task<GPUBenchmarkResult> GetGpuBenchmarkResultsAsObjectAsync()
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(_sampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnGpuAsync()).Bps;
        }
        catch (Exception) { }
        return new GPUBenchmarkResult(ffmpegRating, new(ffmpegRating));
    }

    [SupportedOSPlatform("windows")]
    async Task<List<DriveBenchmarkResult>> GetDrivesBenchmarkResultsAsObjectAsync(int testDataSize)
    {
        var drivesBenchmarkResults = new List<(BenchmarkResult Read, BenchmarkResult Write)>();
        var readWriteBenchmark = new ReadWriteBenchmark(testDataSize);

        foreach (var logicalDiskName in Drive.LogicalDisksNamesFromDistinctDrives)
        {
            Logger.Info($"Running drive benchmark for {logicalDiskName}");
            drivesBenchmarkResults.Add(await readWriteBenchmark.RunAsync(logicalDiskName));
            Logger.Info($"Completed drive benchmark for {logicalDiskName}");
        }

        IEnumerable<DriveBenchmarkResult>? result = null;
        try
        {
            result = Drive.Info.Zip(drivesBenchmarkResults)
                .Select(zip => new DriveBenchmarkResult(zip.First.Id, zip.Second.Write.Bps) { FreeSpace = zip.First.FreeSpace });
        }
        catch { }
        return result?.ToList() ?? new();
    }

    RAMInfo GetRamAsObject()
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
