namespace Common;

public record SoftwareDefinition(string TypeName, string VisualName, ImmutableArray<SoftwareVersionDefinition> Versions, SoftwareRequirements? Requirements, ImmutableArray<SoftwareDefinition> Plugins)
{
    public static IEnumerable<SoftwareDefinition> Flatten(IEnumerable<SoftwareDefinition> software) =>
        software.Concat(software.SelectMany(x => Flatten(x.Plugins)));
}
public record SoftwareVersionDefinition(string Version, string InstallScript);

public record SoftwareRequirements(string? WindowsVersion);