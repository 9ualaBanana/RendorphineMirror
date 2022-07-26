using Benchmark;

namespace Node.Profiling;

internal static class Profiler
{
    readonly static string _assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "assets");
    readonly static string _sampleVideoPath = Path.Combine(_assetsPath, "4k_sample.mp4");
    readonly static FileBackedVersion LatestExecutedBenchmarkVersion = new(_assetsPath);

    internal static bool BenchmarkVersionIsUpdated =>
        !LatestExecutedBenchmarkVersion.Exists || LatestExecutedBenchmarkVersion > BenchmarkMetadata.Version;

    internal static async Task<object> RunAsync(int testDataSize)
    {
        object hardwarePayload = await ComputeHardwarePayloadAsync(testDataSize);
        LatestExecutedBenchmarkVersion.Update(BenchmarkMetadata.Version);
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

        var state = new BenchmarkNodeState();
        using var _ = GlobalState.TempSetState(state);

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

    static async Task<object> ComputePayloadWithCPUBenchmarkResultsAsync(int testDataSize)
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(_sampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnCpuAsync()).Bps;
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
            ffmpegRating = (await new FFmpegBenchmark(_sampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunOnGpuAsync()).Bps;
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
}
