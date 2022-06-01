namespace Hardware;

public readonly record struct GpuClockInfo(
    int? CurrentCoreClock, int? MaxCoreClock,
    int? CurrentMemoryClock, int? MaxMemoryClock)
{
}
