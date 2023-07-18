namespace Node.Plugins;

public interface ISoftwareListProvider
{
    IReadOnlyDictionary<string, SoftwareDefinition> Software { get; }
}
