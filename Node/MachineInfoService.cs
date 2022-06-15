using Benchmark;
using Machine;
using System.Reflection;
using System.Text.Json;

namespace Node;

internal class MachineInfoService
{
    readonly string _sessionId;
    readonly string _nickname;
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

    internal MachineInfoService(string sessionId, string nickname, HttpClient httpClient)
    {
        _sessionId = sessionId;
        _nickname = nickname;
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
            cpu = await ComputePayloadWithCPUBenchmarkResultsAsync(testDataSize),
            gpu = new()
            {
                rating = default,
                ffmpegrating = default,
                load = default,
            },
            ram = GetRAMPayload(),
            disks = await ComputePayloadWithDrivesBenchmarkResultsAsync(testDataSize)
        };
    }

    static async Task<CPUPayload> ComputePayloadWithCPUBenchmarkResultsAsync(uint testDataSize)
    {
        double ffmpegRating = default;
        try
        {
            ffmpegRating = (await new FFmpegBenchmark(SampleVideoPath, $"{Path.Combine(_assetsPath, "ffmpeg")}").RunAsync()).Rate;
        }
        catch (Exception) { }
        return new()
        {
            rating = (await new ZipBenchmark(testDataSize).RunAsync()).Rate,
            ffmpegrating = ffmpegRating,
            load = default,
        };
    }

    static async Task<DrivesPayload[]> ComputePayloadWithDrivesBenchmarkResultsAsync(uint testDataSize)
    {
        var drivesBenchmarkResults = new List<(BenchmarkResult Read, BenchmarkResult Write)>();
        foreach (var logicalDiskName in Drive.LogicalDisksNamesFromDistinctDrives)
        {
            drivesBenchmarkResults.Add(await new ReadWriteBenchmark(testDataSize).RunAsync(logicalDiskName));
        }

        return Drive.Info.Zip(drivesBenchmarkResults)
            .Select(zip => new DrivesPayload()
            {
                freespace = zip.First.FreeSpace,
                writespeed = zip.Second.Write.Rate
            })
            .ToArray();
    }

    static RAMPayload GetRAMPayload()
    {
        var ramInfo = RAM.Info;
        return new()
        {
            total = ramInfo.Aggregate(0ul, (totalCapacity, ramUnit) => totalCapacity += ramUnit.Capacity),
            free = ramInfo.Aggregate(0ul, (freeMemory, ramUnit) => freeMemory += ramUnit.FreeMemory)
        };
    }

    internal async Task SendMachineInfoAsync(string serverUri, BenchmarkResults? hardwarePayload)
    {
        var requestPayload = await BuildPayloadAsync(hardwarePayload);

        try
        {
            var response = await _http.PostAsync(serverUri, requestPayload);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception) { }
    }

    async Task<FormUrlEncodedContent> BuildPayloadAsync(BenchmarkResults? hardwarePayload)
    {
        var payloadContent = new Dictionary<string, string>()
        {
            ["sessionid"] = _sessionId,
            ["nickname"] = _nickname,
            ["ip"] = (await MachineInfo.GetPublicIPAsync()).ToString(),
            ["port"] = MachineInfo.Port,
            ["hardware"] = JsonSerializer.Serialize(hardwarePayload)
        };
        return new FormUrlEncodedContent(payloadContent);
    }
}
