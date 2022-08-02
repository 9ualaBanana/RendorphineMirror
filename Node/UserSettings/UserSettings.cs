using Newtonsoft.Json;

namespace Node.UserSettings;

[JsonConverter(typeof(UserSettingsConverter))]
public class UserSettings
{
    [JsonProperty(ItemConverterType = typeof(PluginToInstallConverter))]
    public List<PluginToInstall> InstallSoftware { get; set; } = new();
    [JsonProperty(ItemConverterType = typeof(PluginToInstallConverter))]
    public List<PluginToInstall> NodeInstallSoftware { get; set; } = new();
}
