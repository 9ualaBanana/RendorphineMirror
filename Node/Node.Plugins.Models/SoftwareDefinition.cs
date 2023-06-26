namespace Node.Plugins.Models;

public record SoftwareDefinition(string VisualName, ImmutableDictionary<PluginVersion, SoftwareVersionDefinition> Versions);
public record SoftwareVersionDefinition(string InstallScript, SoftwareRequirements Requirements);

public record SoftwareRequirements(ImmutableDictionary<PlatformID, SoftwareSupportedPlatform> Platforms, ImmutableArray<SoftwareParent> Parents);
public record SoftwareSupportedPlatform(string? MinVersion);
public record SoftwareParent(string Type, PluginVersion Version);