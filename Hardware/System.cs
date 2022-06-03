using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

namespace Hardware;

public static class System
{
    public static Container Info()
    {
        if (OperatingSystem.IsWindows()) return WindowsInfo();
        throw new PlatformNotSupportedException();
    }

    [SupportedOSPlatform("windows")]
    static Container WindowsInfo()
    {
        using ManagementObjectSearcher systemInfoSearcher = new("SELECT * FROM Win32_ComputerSystem");
        using ManagementObjectCollection systemsInfo = systemInfoSearcher.Get();

        var container = new Container();
        foreach (var systemInfo in systemsInfo) container.Add(systemInfo);
        return container;
    }
}