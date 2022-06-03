using Hardware.MessageBuilders;
using System.ComponentModel;
using System.Management;
using System.Net;
using System.Net.Sockets;
using TelegramHelper;

namespace Hardware;

public readonly record struct HardwareInfo(
    Container System,
    Container CPU,
    IEnumerable<Dictionary<string, object>> GPU,    // Adapts to project-scoped required behaviors.
    Container RAM,
    Container Disks) : IDisposable
{
    readonly public string? Name = ((ManagementObject?)System.Components[0])?["Name"]?.ToString();
    public static async Task<(IPAddress? v4, IPAddress? v6)> IP()
    {
        try
        {
            var ips = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork | AddressFamily.InterNetworkV6);
            return (ips[0], ips[1]);
        }
        catch
        {
            return (null, null);
        }
    }

    public static HardwareInfo Get()
    {
        return new(
            Hardware.System.Info(),
            Hardware.CPU.Info(),
            Hardware.GPU.Info(),
            Hardware.RAM.Info(),
            Hardware.Disks.Info());
    }

    public async Task<string> ToTelegramMessage(bool verbose = false)
    {
        if (OperatingSystem.IsWindows()) return (await new WindowsHardwareInfoMessageBuilder(this).Build(verbose)).Sanitize();
        throw new PlatformNotSupportedException();
    }

    public void Dispose()
    {
        System.Dispose();
        CPU.Dispose();
        RAM.Dispose();
        Disks.Dispose();
    }
}
