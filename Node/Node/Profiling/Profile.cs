namespace Node.Profiling;

public record Profile(
    int Port,
    int WebPort,
    string Nickname,
    string Guid,
    string Version,
    string Ip,
    string? Domain,
    Dictionary<TaskInputType, int> AllowedInputs,
    Dictionary<TaskOutputType, int> AllowedOutputs,
    Dictionary<string, int> AllowedTypes,
    Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> Software,
    object Pricing,
    BenchmarkData Hardware
);
