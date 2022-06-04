using Common;
using Hardware.MessageBuilders;
using System.ComponentModel;
using System.Net;
using TelegramHelper;

namespace Hardware;

public readonly record struct HardwareInfo(
    Container System,
    Container CPU,
    IEnumerable<Dictionary<string, object>> GPU,    // Adapts to project-scoped required behaviors.
    Container RAM,
    Container Disks) : IDisposable
{
    readonly public string Name = Environment.UserName;
    readonly public string Version = Init.Version;
    public static async Task<IPAddress> IP() => await PortForwarding.GetPublicIPAsync();

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
