using Machine.Plugins;
using Machine.Plugins.Deployment;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.UserSettings;

[JsonConverter(typeof(UserSettingsConverter))]
public class UserSettings
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public readonly string Guid;
    [JsonProperty(ItemConverterType = typeof(PluginToDeployConverter))]
    public List<PluginToDeploy> InstallSoftware { get; set; } = new();
    [JsonProperty(ItemConverterType = typeof(PluginToDeployConverter))]
    public List<PluginToDeploy> NodeInstallSoftware { get; set; } = new();


    public UserSettings(string? guid = default) => Guid = guid ?? Settings.Guid!;


    internal async Task TryDeployUninstalledPluginsAsync(PluginsDeployer deployer)
    {
        _logger.Trace("Trying to deploy uninstalled plugins from {List}", nameof(NodeInstallSoftware));
        await PluginsManager.TryDeployUninstalledPluginsAsync(NodeInstallSoftware, deployer);
        _logger.Trace("Trying to deploy uninstalled plugins from {List}", nameof(InstallSoftware));
        await PluginsManager.TryDeployUninstalledPluginsAsync(InstallSoftware, deployer);
    }

    internal static async Task<UserSettings> ReadOrThrowAsync(HttpResponseMessage response)
    {
        try
        {
            var userSettings = await ReadOrThrowAsyncCore(response);
            _logger.Trace("User settings are successfully read from {Response}", nameof(HttpResponseMessage));
            return userSettings;
        }
        catch (Exception ex) { _logger.Error(ex, "User settings couldn't be read from {Response}", nameof(HttpResponseMessage)); throw; }
    }

    static async Task<UserSettings> ReadOrThrowAsyncCore(HttpResponseMessage response)
    {
        var jToken = await Api.GetJsonFromResponseIfSuccessfulAsync(response);
        return ((JObject)jToken).Property("settings")!.Value.ToObject<UserSettings>()!;
    }
}
