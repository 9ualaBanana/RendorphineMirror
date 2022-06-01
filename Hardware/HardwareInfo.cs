using System.ComponentModel;

namespace Hardware;

public readonly record struct HardwareInfo(
    Container CPU,
    Container GPU,
    Container RAM,
    Container Disks)
{
    public static HardwareInfo Get()
    {
        return new(
            Hardware.CPU.Info(),
            Hardware.GPU.Info(),
            Hardware.RAM.Info(),
            Hardware.Disk.Info());
    }
}
