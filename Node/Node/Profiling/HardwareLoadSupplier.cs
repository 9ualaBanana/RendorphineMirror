using System.Net.NetworkInformation;

namespace Node.Profiling;

public static class HardwareLoadSupplier
{
    static long TotalBytesSent = 0;
    static long TotalBytesReceived = 0;

    static HardwareLoadSupplier()
    {
        // to force update TotalBytesSent and TotalBytesReceived
        _ = GetPartial();
    }

    public static HardwareLoadPartial GetPartial()
    {
        var cpuload = 0d;
        var gpuload = 0d;
        var freeram = 0L;
        var internetup = 0L;
        var internetdown = 0L;

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            cpuload = CPU.Info.Select(p => p.LoadPercentage / 100d).Average();
            gpuload = GPU.Info.Select(p => p.LoadPercentage / 100d).Average();
            freeram = RAM.Info.Aggregate(0L, (freeMemory, ramUnit) => freeMemory += (long) ramUnit.FreeMemory);
        }

        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Select(i => i.GetIPv4Statistics())
            .ToArray();

        internetup = interfaces.Sum(i => i.BytesSent) - TotalBytesSent;
        internetdown = interfaces.Sum(i => i.BytesReceived) - TotalBytesReceived;

        TotalBytesSent += internetup;
        TotalBytesReceived += internetdown;

        return new HardwareLoadPartial(cpuload, gpuload, freeram, internetup, internetdown);
    }

    public static HardwareLoad GetFull()
    {
        var partial = GetPartial();

        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed)
            .Where(d =>
            {
                try
                {
                    _ = d.AvailableFreeSpace;
                    return true;
                }
                catch { return false; }
            })
            .Select(d => KeyValuePair.Create(d.Name, new HardwareLoadDrive(d.AvailableFreeSpace)))
            .ToDictionary();

        return new HardwareLoad(partial, drives);
    }
}
