using Machine.Plugins.Deployment;
using Newtonsoft.Json;

namespace Node.UserSettings;

[JsonConverter(typeof(UserSettingsConverter))]
public class UserSettings
{
    public readonly string Guid;
    [JsonProperty(ItemConverterType = typeof(PluginToDeployConverter))]
    public List<PluginToDeploy> InstallSoftware { get; set; } = new();
    [JsonProperty(ItemConverterType = typeof(PluginToDeployConverter))]
    public List<PluginToDeploy> NodeInstallSoftware { get; set; } = new();


    public UserSettings(string? guid = default) => Guid = guid ?? Settings.Guid!;
}
