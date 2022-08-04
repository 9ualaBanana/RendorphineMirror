﻿using Benchmark;

namespace Node.Profiling;

internal static class Benchmark
{
    readonly static string _assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "assets");
    readonly static string _sampleVideoPath = Path.Combine(_assetsPath, "4k_sample.mp4");
    readonly static FileBackedVersion LatestExecutedVersion = new(_assetsPath);

    internal static bool ShouldBeRun => !isRun && IsUpdated;
    static bool isRun;
    /// <remarks>
    /// Evaluated once upon node start.
    /// </remarks>
    static bool IsUpdated =>
        !LatestExecutedVersion.Exists || LatestExecutedVersion < BenchmarkMetadata.Version;
    /// <summary>
    /// true if <see cref="Benchmark"/> wasn't run yet after it had been updated; false, otherwise.
    /// </summary>

    internal static async Task<object> RunAsync(int testDataSize)
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
        benchs["cpu"] = Newtonsoft.Json.Linq.JToken.FromObject(cpu);
        var gpu = await ComputePayloadWithGPUBenchmarkResultsAsync();
        benchs["gpu"] = Newtonsoft.Json.Linq.JToken.FromObject(gpu);
        var ram = GetRAMPayload();
        benchs["ram"] = Newtonsoft.Json.Linq.JToken.FromObject(ram);
        var disks = await ComputePayloadWithDrivesBenchmarkResultsAsync(testDataSize);
        benchs["disks"] = Newtonsoft.Json.Linq.JToken.FromObject(disks);

        isRun = true;
        LatestExecutedVersion.Update(BenchmarkMetadata.Version);

        var output = new
        {
            cpu,
            gpu,
            ram,
            disks,
        };
        Log.Information("Benchmark completed: " + Newtonsoft.Json.JsonConvert.SerializeObject(output, Newtonsoft.Json.Formatting.None));

        return output;
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
