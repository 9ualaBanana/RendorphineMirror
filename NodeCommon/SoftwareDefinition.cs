namespace NodeCommon;

public record SoftwareDefinition(string VisualName, ImmutableDictionary<string, SoftwareVersionDefinition> Versions, SoftwareRequirements? Requirements, ImmutableArray<string> Parents);
public record SoftwareVersionDefinition(string InstallScript);

public record SoftwareRequirements(string? WindowsVersion);