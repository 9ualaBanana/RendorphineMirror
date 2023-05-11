namespace Node.Plugins;

public interface IPluginDiscoverer
{
    ValueTask<IEnumerable<Plugin>> Discover();
}
