namespace Node.Profiling;

public record HardwareLoad(HardwareLoadPartial Load, IReadOnlyDictionary<string, HardwareLoadDrive> Drives);
public record HardwareLoadDrive(long FreeSpace);

public class HardwareLoadPartial
{
    [JsonProperty("cpu")] public double CpuLoad { get; }
    [JsonProperty("gpu")] public double GpuLoad { get; }
    [JsonProperty("ram")] public long FreeRam { get; }
    [JsonProperty("iup")] public long InternetUp { get; }
    [JsonProperty("idown")] public long InternetDown { get; }

    public HardwareLoadPartial(double cpuLoad, double gpuLoad, long freeRam, long internetUp, long internetDown)
    {
        CpuLoad = cpuLoad;
        GpuLoad = gpuLoad;
        FreeRam = freeRam;
        InternetUp = internetUp;
        InternetDown = internetDown;
    }
}
