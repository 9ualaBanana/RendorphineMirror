using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.UserSettings;

public class UserSettingsConverter : JsonConverter<UserSettings>
{
    public override UserSettings? ReadJson(JsonReader reader, Type objectType, UserSettings? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = (JObject)JObject.Load(reader).Property("settings")!.Value;
        var userSettings = new UserSettings();

        var installSoftware = (JObject?)jObject.Property("installsoftware")?.Value;
        if (installSoftware is not null)
            ReadAll(installSoftware, userSettings.InstallSoftware);

        var allNodeInstallSoftware = (JObject?)jObject.Property("nodeinstallsoftware")?.Value;
        var nodeInstallSoftware = (JObject?)allNodeInstallSoftware?.Property(Settings.Guid!)?.Value;
        if (nodeInstallSoftware is not null)
            ReadAll(nodeInstallSoftware, userSettings.NodeInstallSoftware);

        return userSettings;
    }

    static void ReadAll(JObject pluginsJObject, IList<PluginToInstall> deserializedPlugins)
    {
        var plugins = pluginsJObject.Properties();
        while (pluginsJObject.HasValues)
        {
            deserializedPlugins.Add(plugins.First().ToObject<PluginToInstall>()!);
            pluginsJObject.Properties().First().Remove();
        }
    }

    public override void WriteJson(JsonWriter writer, UserSettings? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        if (value is not null) WriteValue(writer, value);
        writer.WriteEndObject();
    }

    static void WriteValue(JsonWriter writer, UserSettings value)
    {
        if (value.InstallSoftware.Any())
        {
            writer.WritePropertyName("installsoftware");
            writer.WriteStartObject();
            WritePlugins(writer, value.InstallSoftware);
            writer.WriteEndObject();
        }
        if (value.NodeInstallSoftware.Any())
        {
            writer.WritePropertyName("nodeinstallsoftware");
            writer.WriteStartObject();
            WritePlugins(writer, value.NodeInstallSoftware);
            writer.WriteEndObject();
        }
    }

    static void WritePlugins(JsonWriter writer, IEnumerable<PluginToInstall> plugins)
    {
        foreach (var plugin in plugins.GroupBy(plugin => plugin.Type))
        {
            writer.WritePropertyName(plugin.Key);
            writer.WriteStartObject();
            foreach (var pluginVersion in plugin)
                WritePlugin(writer, pluginVersion);
            writer.WriteEndObject();
        }
    }

    static void WritePlugin(JsonWriter writer, PluginToInstall plugin)
    {
        writer.WritePropertyName(plugin.Version);
        writer.WriteStartObject();
        writer.WritePropertyName("plugins");
        writer.WriteStartObject();
        if (plugin.SubPlugins is not null)
            WriteSubPlugins(writer, plugin.SubPlugins);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    static void WriteSubPlugins(JsonWriter writer, IEnumerable<PluginToInstall> subPlugins)
    {
        foreach (var subPlugin in subPlugins)
            WriteSubPlugin(writer, subPlugin);
    }

    static void WriteSubPlugin(JsonWriter writer, PluginToInstall subPlugin)
    {
        writer.WritePropertyName(subPlugin.Type);
        writer.WriteStartObject();
        writer.WritePropertyName("version");
        writer.WriteValue(subPlugin.Version);
        writer.WritePropertyName("subplugins");
        writer.WriteStartObject();
        if (subPlugin.SubPlugins is not null)
            WriteSubSubPlugins(writer, subPlugin.SubPlugins);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    static void WriteSubSubPlugins(JsonWriter writer, IEnumerable<PluginToInstall> subSubPlugins)
    {
        foreach (var subSubPlugin in subSubPlugins)
        {
            writer.WritePropertyName(subSubPlugin.Type);
            writer.WriteValue(subSubPlugin.Version);
        }
    }
}
