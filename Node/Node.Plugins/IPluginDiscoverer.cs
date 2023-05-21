namespace Node.Plugins;

public interface IPluginDiscoverer
{
    Task<IEnumerable<Plugin>> DiscoverAsync();
}
