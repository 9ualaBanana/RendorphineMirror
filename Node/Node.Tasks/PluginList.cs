namespace Node.Tasks;

public class PluginList
{
    public IReadOnlyCollection<Plugin> Plugins { get; }

    public PluginList(IReadOnlyCollection<Plugin> plugins) => Plugins = plugins;


    public Plugin GetPlugin(PluginType type) => TryGetPlugin(type).ThrowIfNull();
    public Plugin? TryGetPlugin(PluginType type) => Plugins.Where(p => p.Type == type).MaxBy(p => p.Version);
}
