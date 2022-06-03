using Hardware.MessageBuilders;
using System.ComponentModel;
using System.Management;
using TelegramHelper;

namespace Hardware;

public readonly record struct HardwareInfo(
    Container System,
    Container CPU,
    IEnumerable<Dictionary<string, object>> GPU,    // Adapts to project-scoped required behaviors.
    Container RAM,
    Container Disks,
    Container Network) : IDisposable
{
    readonly public string? Name = ((ManagementObject?)System.Components[0])?["Name"]?.ToString();

    public static HardwareInfo Get()
    {
        return new(
            Hardware.System.Info(),
            Hardware.CPU.Info(),
            Hardware.GPU.Info(),
            Hardware.RAM.Info(),
            Hardware.Disks.Info(),
            Hardware.Network.Info());
    }

    public string ToTelegramMessage(bool verbose = false)
    {
        if (OperatingSystem.IsWindows()) return new WindowsHardwareInfoMessageBuilder(this).Build(verbose).Sanitize();
        throw new PlatformNotSupportedException();
    }

    public void Dispose()
    {
        System.Dispose();
        CPU.Dispose();
        RAM.Dispose();
        Disks.Dispose();
        Network.Dispose();
    }
}
