using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

namespace Hardware;

public static class Network
{
    public static Container Info()
    {
        if (OperatingSystem.IsWindows()) return WindowsInfo();
        throw new PlatformNotSupportedException();
    }

    [SupportedOSPlatform("windows")]
    static Container WindowsInfo()
    {
        using ManagementObjectSearcher networkSearcher = new("SELECT * FROM Win32_NetworkAdapterConfiguration");
        using ManagementObjectCollection networks = networkSearcher.Get();

        var container = new Container();
        foreach (var network in networks) container.Add(network);
        return container;
    }
}
