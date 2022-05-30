using Hardware;

var allGpus = GpuInfo.GetForAll();
var cpus = CpuInfo.GetForAll();
var cpu = CpuInfo.GetFor("BFEBFBFF000906EA");
var rams = RamInfo.GetForAll();
var ram = RamInfo.GetFor("F3375529");
var disks = DiskInfo.GetForAll();
var disk = DiskInfo.GetFor("9EAB7761");

Console.ReadLine();