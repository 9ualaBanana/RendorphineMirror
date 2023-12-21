namespace Node.Profiling;

public record HardwareLoad(HardwareLoadPartial Load, IReadOnlyDictionary<string, HardwareLoadDrive> Drives);
public record HardwareLoadDrive(long FreeSpace);

public record HardwareLoadPartial(
    double CpuLoad,
    double GpuLoad,
    long FreeRam,
    long InternetUp,
    long InternetDown
);
