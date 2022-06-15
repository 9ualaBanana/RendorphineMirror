using System.Management;
using System.Runtime.Versioning;

namespace Machine;

[SupportedOSPlatform("Windows")]
internal static class WindowsRAM
{

    internal static List<RAM> Info()
    {
        using var ramSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
        using var ramMbos = ramSearcher.Get();

        var ramUnits = new List<RAM>(ramMbos.Count);
        foreach (var ramMbo in ramMbos)
        {
            _ = uint.TryParse(ramMbo["ConfiguredClockSpeed"].ToString(), out var speed);
            _ = ulong.TryParse(ramMbo["Capacity"].ToString(), out var capacity);
            var deviceLocator = ramMbo["DeviceLocator"].ToString()!;
            var serialNumber = ramMbo["SerialNumber"].ToString()!;

            ramUnits.Add(new(speed, capacity, FreeMemory, deviceLocator, serialNumber));
        }

        return ramUnits;
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

            return availableMemory;
        }
    }
}
