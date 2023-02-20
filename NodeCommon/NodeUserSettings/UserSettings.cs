using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon.NodeUserSettings;

[JsonConverter(typeof(UserSettingsConverter))]
public class UserSettings
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public string? Guid;
    [JsonProperty(ItemConverterType = typeof(PluginToDeployConverter))]
    public HashSet<PluginToDeploy> InstallSoftware { get; set; } = new();
    [JsonProperty(ItemConverterType = typeof(PluginToDeployConverter))]
    public Dictionary<string, HashSet<PluginToDeploy>> NodeInstallSoftware { get; set; } = new();
    public HashSet<PluginToDeploy> ThisNodeInstallSoftware => NodeInstallSoftwareFor(Guid ?? string.Empty);
    public HashSet<PluginToDeploy> NodeInstallSoftwareFor(string guid) => NodeInstallSoftware.GetValueOrDefault(guid, new());


    public UserSettings(string? guid = default) => Guid = guid;

    public static async Task<UserSettings> ReadOrThrowAsync(HttpResponseMessage response)
    {
        try
        {
            var userSettings = await ReadOrThrowAsyncCore(response);
            _logger.Trace("User settings are successfully read from {Response}", nameof(HttpResponseMessage)); userSettings.LogPlugins();
            return userSettings;
        }
        catch (Exception ex) { _logger.Error(ex, "User settings couldn't be read from {Response}", nameof(HttpResponseMessage)); throw; }
    }

    static async Task<UserSettings> ReadOrThrowAsyncCore(HttpResponseMessage response)
    {
        var jToken = await Api.GetJsonIfSuccessfulAsync(response);
        var settings = ((JObject)jToken).Property("settings")!.Value;
        return settings.HasValues ? settings.ToObject<UserSettings>()! : new();
    }

    void LogPlugins()
    {
        _logger.Trace("{PluginsList}: {Plugins}", nameof(NodeInstallSoftware), string.Join(", ", ThisNodeInstallSoftware.SelectMany(plugin => plugin.SelfAndSubPlugins).Select(plugin => plugin.Type)));
        _logger.Trace("{PluginsList}: {Plugins}", nameof(InstallSoftware), string.Join(", ", InstallSoftware.SelectMany(plugin => plugin.SelfAndSubPlugins).Select(plugin => plugin.Type)));
    }
}
