﻿using System.Xml;

namespace Node.Plugins.Discoverers;

internal class NvidiaDriverPluginDiscoverer : IPluginDiscoverer
{
    public async Task<IEnumerable<Plugin>> DiscoverAsync()
    {
        var nvidiasmi = await Processes.FullExecute("nvidia-smi", "-q -x", PluginDiscoverer.Logger.AsLoggable(), LogLevel.Off);
        var xml = new XmlDocument().With(xml => xml.LoadXml(nvidiasmi));
        var version = (xml["nvidia_smi_log"]?["driver_version"]?.FirstChild?.Value).ThrowIfNull();

        return new[] { new Plugin(PluginType.NvidiaDriver, version, string.Empty) };
    }
}