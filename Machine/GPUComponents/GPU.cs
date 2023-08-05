using System.Diagnostics;

namespace Machine;

public record GPU(
    Guid UUID,
    string Name,
    ulong TotalMemory,
    ulong UsedMemory,
    ulong MaxCoreClockSpeed,
    ulong CurrentCoreClockSpeed,
    ulong MaxMemoryClockSpeed,
    ulong CurrentMemoryClockSpeed,
    uint LoadPercentage)
{
    public static List<GPU> Info
    {
        get
        {
            if (IsNvidia) return NvidiaGPU.Info;
            else return Stub;
            throw new NotSupportedException("Installed GPU is not supported.");
        }
    }

    static bool IsNvidia
    {
        get
        {
            try
            {
                Process.Start(new ProcessStartInfo("nvidia-smi", "--version") { CreateNoWindow = true });
                return true;
            }
            catch { return false; }
        }
    }

    static List<GPU> Stub => new(1) { new(Guid.NewGuid(), "Stub", 0, 0, 0, 0, 0, 0, 0) };
}
