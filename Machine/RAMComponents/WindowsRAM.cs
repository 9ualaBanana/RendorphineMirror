using System.Management;
using System.Runtime.Versioning;

namespace Machine;

[SupportedOSPlatform("windows")]
internal static class WindowsRAM
{

    internal static List<RAM> Info
    {
        get
        {
            using var ramSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            using var ramMbos = ramSearcher.Get();

            var ramUnits = new List<RAM>(ramMbos.Count);
            foreach (var ramMbo in ramMbos)
                ramUnits.Add(ToRAM(ramMbo));
            return ramUnits;
        }
    }

    internal static RAM ToRAM(ManagementBaseObject mbo)
    {
        _ = uint.TryParse(mbo["ConfiguredClockSpeed"].ToString(), out var speed);
        _ = ulong.TryParse(mbo["Capacity"].ToString(), out var capacity);
        var deviceLocator = mbo["DeviceLocator"].ToString()!;
        var serialNumber = mbo["SerialNumber"].ToString()!;

        return new(speed, capacity, FreeMemory, deviceLocator, serialNumber);
    }

    static ulong FreeMemory
    {
        get
        {
            using var operatingSystemSeracher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
            using var operatingSystemMbos = operatingSystemSeracher.Get();

            ulong availableMemory = default;
            foreach (var operatingSystemMbo in operatingSystemMbos)
                _ = ulong.TryParse(operatingSystemMbo["FreePhysicalMemory"].ToString(), out availableMemory);

            // FreePhysicalMemory returns megabytes and we need bytes.
            return availableMemory * 1024;
        }
    }
}
