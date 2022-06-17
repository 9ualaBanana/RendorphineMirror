using System.Diagnostics;

namespace Machine;

public record GPU(
    Guid UUID,
    string Name,
    ulong TotalMemory,
    ulong UsedMemory,
    ulong MaxCoreClock,
    ulong CurrentCoreClock,
    ulong MaxMemoryClock,
    ulong CurrentMemoryClock)
{
    public static List<GPU> Info
    {
        get
        {
            if (IsNvidia) return NvidiaGPU.Info;
            throw new NotSupportedException("Installed GPU is not supported.");
        }
    }

    static bool IsNvidia => Process.Start(new ProcessStartInfo("nvidia-smi", "--version") { CreateNoWindow = true }) is not null;
}
