namespace NodeCommon;

public record NodeInfo(string Id, string UserId, string Ip, string Host, ulong LastHearbeat, NodeInfoInfo Info);
public record NodeInfoInfo(string Ip, int Port, int WebPort, string Nickname, string Guid, string Version,
    ImmutableDictionary<string, int> AllowedInputs, ImmutableDictionary<string, int> AllowedOutputs, ImmutableDictionary<string, int> AllowedTypes);

public record NodeInfoPricing(ImmutableDictionary<string, decimal> MinUnitPrice, decimal MinBwPrice, decimal MinStoragePrice);
