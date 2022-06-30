namespace Machine.Plugins.Discoverers;

public interface IPluginDiscoverer
{
    internal IEnumerable<Plugin> Discover();
}
