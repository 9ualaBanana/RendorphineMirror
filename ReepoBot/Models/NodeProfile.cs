using System.Text.Json;

namespace ReepoBot.Models;

public class NodeProfile
{
    public string sessionid { get; init; } = null!;
    public string info { get; init; } = null!;
}

public class Info
{
    public string ip { get; init; } = null!;
    public int port { get; init; }
    public string nickname { get; init; } = null!;
    public string version { get; init; } = null!;
    public IDictionary<string, int> allowedinputs { get; init; } = null!;
    public IDictionary<string, int> allowedoutputs { get; init; } = null!;
    public IDictionary<TaskType, int> allowedtypes { get; init; } = null!;
    public Pricing pricing { get; init; } = null!;
    public JsonDocument software { get; init; } = null!;
    public Hardware? hardware { get; init; }
}

public class Pricing
{
    public IDictionary<string, double> minunitprice { get;init; } = null!;
    public double minbwprice { get; init; }
    public double minstorageprice { get; init; }
}

public class Hardware
{
    public Cpu cpu { get; init; } = null!;
    public Gpu gpu { get; init; } = null!;
    public Ram ram { get; init; } = null!;
    public Disks[] disks { get; init; } = null!;
}

public class Cpu
{
    public double rating { get; init; }
    public IDictionary<string, double> pratings { get; init; } = null!;
    public int load { get; init; }
}

public class Gpu
{
    public double rating { get; init; }
    public IDictionary<string, double> pratings { get; init; } = null!;
    public int load { get; init; }
}

public class Ram
{
    public ulong total { get; init; }
    public ulong free { get; init; }
}

public class Disks
{
    public ulong freespace { get; init; }
    public double writespeed { get; init; }
}
