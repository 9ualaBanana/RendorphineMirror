namespace Node.Plugins;

public interface ISoftwareList
{
    IReadOnlyDictionary<string, SoftwareDefinition> Software { get; }
}
