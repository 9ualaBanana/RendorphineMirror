namespace Node.Plugins.Models;

public record SoftwareVersionInfo(PluginType Type, string Version, string Name, SoftwareVersionInfo.FilesInfo? Files, SoftwareVersionInfo.InstallationInfo? Installation, SoftwareVersionInfo.RequirementsInfo Requirements)
{
    public record FilesInfo(string Main);

    public record InstallationInfo(string Source, string? Script, InstallationInfo.PythonInfo? Python)
    {
        public record PythonInfo(string Version, PythonInfo.PipInfo Pip, PythonInfo.CondaInfo Conda)
        {
            public record PipInfo(ImmutableArray<string> RequirementFiles, ImmutableArray<string> Requirements);
            public record CondaInfo(ImmutableArray<string> Requirements, ImmutableArray<string> Channels);
        }
    }

    public record RequirementsInfo(ImmutableArray<string> Platforms, ImmutableArray<RequirementsInfo.ParentInfo> Parents)
    {
        public record ParentInfo(string Type, string? Version);
    }
}

/*
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
*/
