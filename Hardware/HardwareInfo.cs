using Hardware.MessageBuilders;
using System.ComponentModel;

namespace Hardware;

public readonly record struct HardwareInfo(
    Container CPU,
    Container GPU,
    Container RAM,
    Container Disks,
    Container Network) : IDisposable
{
    public static HardwareInfo Get()
    {
        return new(
            Hardware.CPU.Info(),
            Hardware.GPU.Info(),
            Hardware.RAM.Info(),
            Hardware.Disks.Info(),
            Hardware.Network.Info());
    }

    public string ToTelegramMessage(bool verbose = false)
    {
        if (OperatingSystem.IsWindows()) return new WindowsHardwareInfoMessageBuilder(this).Build(verbose);
        throw new PlatformNotSupportedException();
    }

    public void Dispose()
    {
        CPU.Dispose();
        GPU.Dispose();
        RAM.Dispose();
        Disks.Dispose();
        Network.Dispose();
    }
}
