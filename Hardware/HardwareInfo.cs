using Common;
using Hardware.MessageBuilders;
using System.ComponentModel;
using System.Net;

namespace Hardware;

public readonly record struct HardwareInfo(
    Container CPU,
    IEnumerable<Dictionary<string, object>> GPU,    // Adapted to project-scoped required behaviors.
    Container RAM,
    Container Disks) : IDisposable
{
    readonly public static string UserName = Environment.UserName;
    readonly public static string PCName = Environment.MachineName;
    readonly public static string Version = Init.Version;
    public static async Task<IPAddress> GetIPAsync() => await PortForwarding.GetPublicIPAsync();
    readonly public static string Port = PortForwarding.Port.ToString();
    public static async Task<string> GetBriefInfoAsync() => $"{PCName} {UserName} (v.{Version}) | {await GetIPAsync()}:{Port}";

    public static HardwareInfo Get()
    {
        return new(
            Hardware.CPU.Info(),
            Hardware.GPU.Info(),
            Hardware.RAM.Info(),
            Hardware.Disks.Info());
    }

    public async Task<string> ToTelegramMessageAsync(bool verbose = false)
    {
        if (OperatingSystem.IsWindows()) return (await new WindowsHardwareInfoMessageBuilder(this).BuildAsync(verbose));
        throw new PlatformNotSupportedException();
    }

    public void Dispose()
    {
        CPU.Dispose();
        RAM.Dispose();
        Disks.Dispose();
    }
}
