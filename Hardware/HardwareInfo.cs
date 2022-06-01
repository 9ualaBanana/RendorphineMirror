namespace Hardware;

public readonly record struct HardwareInfo(
    List<CpuInfo> CpuInfo,
    List<GpuInfo> GpuInfo,
    List<RamInfo> RamInfo,
    List<DiskInfo> DiskInfo)
{
    public async static Task<HardwareInfo> GetForAll()
    {
        var cpuInfo = await Hardware.CpuInfo.GetForAll();
        var gpuInfo = Hardware.GpuInfo.GetForAll();
        var diskInfo = await Hardware.DiskInfo.GetForAll();
        var ramInfo = await Hardware.RamInfo.GetForAll();

        return new HardwareInfo(cpuInfo, gpuInfo, ramInfo, diskInfo);
    }
}
