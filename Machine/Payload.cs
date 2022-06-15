namespace Hardware;

public class MachineInfoPayload
{
    public string sessionid { get; set; }
    public string nickname { get; set; }
    public string ip { get; set; }
    public int port { get; set; }
    public string? hardware { get; set; }
}

public class BenchmarkResults
{
    public CPUPayload cpu { get; set; }
    public GPUPayload gpu { get; set; }
    public RAMPayload ram { get; set; }
    public DrivesPayload[] disks { get; set; }
}

public class CPUPayload
{
    public double rating { get; set; }
    public double ffmpegrating { get; set; }
    public int load { get; set; }
}

public class GPUPayload
{
    public double rating { get; set; }
    public double ffmpegrating { get; set; }
    public int load { get; set; }
}

public class RAMPayload
{
    public ulong total { get; set; }
    public ulong free { get; set; }
}

public class DrivesPayload
{
    public ulong freespace { get; set; }
    public double writespeed { get; set; }
}
