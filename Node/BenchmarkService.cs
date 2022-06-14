using Benchmark;
using System.Management;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Node;

internal class BenchmarkService
{
    readonly uint _testDataSize;
    readonly HttpClient _http;

    readonly static string _assetsPath = Path.Combine(GetCodeFileDirectory(), "assets");
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

    internal BenchmarkService(uint testDataSize, HttpClient httpClient)
    {
        _testDataSize = testDataSize;
        _http = httpClient;
    }

    [SupportedOSPlatform("windows")]
    internal async Task SendBenchmarkResultsAsync(string serverUri)
    {
        HardwarePayload? hardwarePayload = null;

        var currentBenchmarkVersion = Assembly.GetAssembly(typeof(BenchmarkResult))!.GetName().Version;
        if (currentBenchmarkVersion != LastExecutedBenchmarkVersion)
        {
            hardwarePayload = await ComputeHardwarePayloadAsync();
            LastExecutedBenchmarkVersion = currentBenchmarkVersion;
        }

        var jsonPayload = await BuildJSONPayloadAsync(hardwarePayload);

        await _http.PostAsJsonAsync(serverUri, jsonPayload);
    }

    [SupportedOSPlatform("windows")]
    async Task<HardwarePayload> ComputeHardwarePayloadAsync()
    {
        var cpuPayload = await ComputePayloadWithCPUBenchmarkResultsAsync();
        var disksPayload = await ComputePayloadWithDisksBenchmarkResultsAsync();
        var ramPayload = GetRAMPayload();

        return new() { cpu = cpuPayload, disks = disksPayload, ram = ramPayload };
    }

    async Task<CPUPayload> ComputePayloadWithCPUBenchmarkResultsAsync()
    {
        return new()
        {
            rating = (await new ZipBenchmark(_testDataSize).RunAsync()).Rate,
            ffmpegrating = (await new FFmpegBenchmark(SampleVideoPath).RunAsync()).Rate
        };
    }

    [SupportedOSPlatform("windows")]
    async Task<DisksPayload[]> ComputePayloadWithDisksBenchmarkResultsAsync()
    {
        var disksBenchmarkResults = new List<(BenchmarkResult Read, BenchmarkResult Write)>();
        foreach (var driveName in ReadWriteBenchmark.DriveNamesFromDistinctDisks)
        {
            disksBenchmarkResults.Add(await new ReadWriteBenchmark(_testDataSize).RunAsync(driveName));
        }
        DisksPayload[] disksPayloads = new DisksPayload[disksBenchmarkResults.Count];
        for (var diskId = 0; diskId < disksBenchmarkResults.Count; diskId++)
        {
            disksPayloads[diskId] = new()
            {
                freespace = Disks.GetFreeSpaceOnDisk(diskId.ToString()),
                writespeed = disksBenchmarkResults[diskId].Write.Rate
            };
        }
        return disksPayloads;
    }

    [SupportedOSPlatform("windows")]
    static RAMPayload GetRAMPayload()
    {
        long totalRamCapacity = 0;
        foreach (var ramComponent in RAM.Info().Components)
        {
            totalRamCapacity += long.Parse(((ManagementBaseObject)ramComponent)["Capacity"].ToString()!);
        }
        return new() { total = totalRamCapacity };
    }

    async static Task<JSONPayload> BuildJSONPayloadAsync(HardwarePayload? hardwarePayload)
    {
        var publicIp = await HardwareInfo.GetPublicIPAsync();
        return new()
        {
            nickname = HardwareInfo.UserName,
            ip = publicIp.ToString(),
            port = int.Parse(HardwareInfo.Port),
            publicip = publicIp == System.Net.IPAddress.None ? 0 : 1,
            hardware = hardwarePayload
        };
    }

    static string SampleVideoPath => Path.Combine(_assetsPath, "4k_sample_long.mp4");
    static string GetCodeFileDirectory([CallerFilePath] string? path = null)
    {
        return Path.GetDirectoryName(path)!;
    }


    class JSONPayload
    {
        public string sessionid = Guid.NewGuid().ToString();
        public string nickname { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public int publicip { get; set; }
        public HardwarePayload? hardware { get; set; }
    }

    public class HardwarePayload
    {
        public CPUPayload cpu { get; set; }
        public GPUPayload gpu { get; set; }
        public RAMPayload ram { get; set; }
        public DisksPayload[] disks { get; set; }
    }

    public class CPUPayload
    {
        public double rating { get; set; }
        public double ffmpegrating { get; set; }
    }

    public class GPUPayload
    {
        public double rating { get; set; }
        public double ffmpegrating { get; set; }
    }

    public class RAMPayload
    {
        public long total { get; set; }
    }

    public class DisksPayload
    {
        public long freespace { get; set; }
        public double writespeed { get; set; }
    }

}
