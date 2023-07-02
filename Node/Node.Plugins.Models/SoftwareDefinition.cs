namespace Node.Plugins.Models;

public sealed record SoftwareDefinition(string VisualName, ImmutableDictionary<PluginVersion, SoftwareVersionDefinition> Versions);
public sealed record SoftwareVersionDefinition(SoftwareInstallation Installation, SoftwareRequirements Requirements);

public sealed record SoftwareRequirements(ImmutableDictionary<PlatformID, SoftwareSupportedPlatform> Platforms, ImmutableArray<SoftwareParent> Parents);
public sealed record SoftwareSupportedPlatform(string? MinVersion);
public sealed record SoftwareParent(string Type, string Version);


public sealed class SoftwareInstallation
{
    public string? Script { get; init; }
    public SoftwareCondaEnvInfo? CondaEnvInfo { get; init; }
}

public sealed record SoftwareCondaEnvInfo(string PythonVersion, ImmutableArray<string> Requirements, ImmutableArray<string> Channels, ImmutableArray<string>? PipRequirements);