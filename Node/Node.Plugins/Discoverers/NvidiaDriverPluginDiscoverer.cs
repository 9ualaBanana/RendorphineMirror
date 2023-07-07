using System.Xml;

namespace Node.Plugins.Discoverers;

internal class NvidiaDriverPluginDiscoverer : IPluginDiscoverer
{
    public async Task<IEnumerable<Plugin>> DiscoverAsync()
    {
        // 535.54.03
        var nvidiasmi = await new ProcessLauncher("nvidia-smi", "-q", "-x")
            .ExecuteFullAsync();

        var xml = new XmlDocument().With(xml => xml.LoadXml(nvidiasmi));
        var version = (xml["nvidia_smi_log"]?["driver_version"]?.FirstChild?.Value).ThrowIfNull();

        return new[] { new Plugin(PluginType.NvidiaDriver, version, string.Empty) };
    }
}