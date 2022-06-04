using Common;
using Hardware.MessageBuilders;
using System.ComponentModel;
using System.Net;
using TelegramHelper;

namespace Hardware;

public readonly record struct HardwareInfo(
    Container CPU,
    IEnumerable<Dictionary<string, object>> GPU,    // Adapted to project-scoped required behaviors.
    Container RAM,
    Container Disks) : IDisposable
{
    readonly public static string Name = Environment.UserName;
    readonly public static string Version = Init.Version;
    public static async Task<IPAddress> GetIPAsync() => await PortForwarding.GetPublicIPAsync();
    public static async Task<string> GetBriefAsync() => $"{Name} | {Version} | {await GetIPAsync()}";

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
        if (OperatingSystem.IsWindows()) return (await new WindowsHardwareInfoMessageBuilder(this).Build(verbose)).Sanitize();
        throw new PlatformNotSupportedException();
    }

    public void Dispose()
    {
        CPU.Dispose();
        RAM.Dispose();
        Disks.Dispose();
    }
}
