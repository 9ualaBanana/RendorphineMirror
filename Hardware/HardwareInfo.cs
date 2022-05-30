namespace Hardware;

public readonly record struct HardwareInfo(
    List<CpuInfo> CpuInfo,
    List<GpuInfo> GpuInfo,
    List<RamInfo> RamInfo,
    List<DiskInfo> DiskInfo)
{
    public static HardwareInfo GetForAll()
    {
        var cpuInfo = Hardware.CpuInfo.GetForAll();
        var gpuInfo = Hardware.GpuInfo.GetForAll();
        var ramInfo = Hardware.RamInfo.GetForAll();
        var diskInfo = Hardware.DiskInfo.GetForAll();
        return new HardwareInfo(cpuInfo, gpuInfo, ramInfo, diskInfo);
    }
}
