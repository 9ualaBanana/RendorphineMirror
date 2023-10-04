namespace Node.Plugins.Models;

public record SoftwareVersionInfo(PluginType Type, string Version, string Name, SoftwareVersionInfo.FilesInfo? Files, SoftwareVersionInfo.InstallationInfo? Installation, SoftwareVersionInfo.RequirementsInfo Requirements)
{
    public record FilesInfo(string Main);

    public record InstallationInfo([property: JsonConverter(typeof(InstallationInfo.SourceInfo.JsonConverter))] InstallationInfo.SourceInfo Source, string? Script, InstallationInfo.PythonInfo? Python)
    {
        public record SourceInfo(SourceInfo.SourceType Type)
        {
            public enum SourceType
            {
                Registry,
                Url
            }

            public class JsonConverter : JsonConverter<SourceInfo>
            {
                public override bool CanWrite => false;

                public override void WriteJson(JsonWriter writer, SourceInfo? value, JsonSerializer serializer) => throw new NotImplementedException();
                public override SourceInfo? ReadJson(JsonReader reader, Type objectType, SourceInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
                {
                    var jobj = JObject.Load(reader);
                    var type = jobj.Property("type", StringComparison.OrdinalIgnoreCase).ThrowIfNull().Value.ToObject<SourceType>();

                    return type switch
                    {
                        SourceType.Registry => jobj.ToObject<RegistrySourceInfo>(),
                        SourceType.Url => jobj.ToObject<UrlSourceInfo>(),
                        _ => throw new InvalidOperationException("Unknown type " + jobj.Property("type", StringComparison.OrdinalIgnoreCase)),
                    };
                }
            }
        }
        public record RegistrySourceInfo(SourceInfo.SourceType Type) : SourceInfo(Type);
        public record UrlSourceInfo(SourceInfo.SourceType Type, string Url) : SourceInfo(Type);


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
