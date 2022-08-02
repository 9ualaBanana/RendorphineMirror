using Newtonsoft.Json;

namespace Node.UserSettings;

[JsonConverter(typeof(PluginToInstallConverter))]
public class PluginToInstall
{
    public string Type { get; set; } = null!;
    public string Version { get; set; } = null!;
    public IEnumerable<PluginToInstall>? SubPlugins { get; set; }
}
