namespace Node.Profiling;

public class Profile
{
    public int Port => Settings.UPnpPort;
    public int WebPort => Settings.UPnpServerPort;
    public string Nickname => Settings.NodeName;
    public string Guid => Settings.Guid;
    public string Version => MachineInfo.Version;

#pragma warning disable CS8618 // Properties are not set
    public string Ip { get; set; }
    public Dictionary<TaskInputType, int> AllowedInputs { get; set; }
    public Dictionary<TaskOutputType, int> AllowedOutputs { get; set; }
    public Dictionary<string, int> AllowedTypes { get; set; }
    public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> Software { get; set; }
    public object Pricing { get; set; }
    public BenchmarkData Hardware { get; set; }
#pragma warning restore
}
