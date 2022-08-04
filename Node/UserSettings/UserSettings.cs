using Machine.Plugins.Deployment;
using Newtonsoft.Json;

namespace Node.UserSettings;

[JsonConverter(typeof(UserSettingsConverter))]
public class UserSettings
{
    [JsonProperty(ItemConverterType = typeof(PluginToDeployConverter))]
    public List<PluginToDeploy> InstallSoftware { get; set; } = new();
    [JsonProperty(ItemConverterType = typeof(PluginToDeployConverter))]
    public List<PluginToDeploy> NodeInstallSoftware { get; set; } = new();
}
