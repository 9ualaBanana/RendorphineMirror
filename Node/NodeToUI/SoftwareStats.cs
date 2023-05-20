namespace NodeToUI;

public record SoftwareStats(ulong Total, ImmutableDictionary<string, SoftwareStatsByVersion> ByVersion);
public record SoftwareStatsByVersion(ulong Total); // TODO: byplugin
