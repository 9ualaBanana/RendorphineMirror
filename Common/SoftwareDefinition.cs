namespace Common;

public record SoftwareDefinition(string TypeName, string VisualName, ImmutableArray<SoftwareVersionDefinition> Versions, SoftwareRequirements? Requirements, ImmutableArray<SoftwareDefinition> Plugins);
public record SoftwareVersionDefinition(string Version, string InstallScript);

public record SoftwareRequirements(string? WindowsVersion);