namespace Node.Plugins.Discoverers;

public interface IPluginDiscoverer
{
    ValueTask<IEnumerable<Plugin>> Discover();
}
