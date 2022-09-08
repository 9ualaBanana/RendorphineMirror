using Node.Plugins.Deployment;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.UserSettings;

public class UserSettingsConverter : JsonConverter<UserSettings>
{
    public override UserSettings? ReadJson(JsonReader reader, Type objectType, UserSettings? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var userSettings = new UserSettings();

        var installSoftware = (JObject?)jObject.Property("installsoftware")?.Value;
        ReadAll(installSoftware, userSettings.InstallSoftware);

        var nodeInstallSoftwareForAllNodes= (JObject?)jObject.Property("nodeinstallsoftware")?.Value;
        if (nodeInstallSoftwareForAllNodes is not null)
        {
            foreach (var nodeSpecificSoftwareProperty in nodeInstallSoftwareForAllNodes.Properties()!)
            {
                userSettings.NodeInstallSoftware.Add(nodeSpecificSoftwareProperty.Name, new());
                ReadAll((JObject?)nodeSpecificSoftwareProperty.Value, userSettings.NodeInstallSoftware[nodeSpecificSoftwareProperty.Name]);
            }
        }

        return userSettings;
    }

    static void ReadAll(JObject? pluginsJObject, HashSet<PluginToDeploy> deserializedPlugins)
    {
        if (pluginsJObject is null) return;

        var plugins = pluginsJObject.Properties();
        while (pluginsJObject.HasValues)
        {
            deserializedPlugins.Add(plugins.First().ToObject<PluginToDeploy>()!);
            pluginsJObject.Properties().First().Remove();
        }
    }

    public override void WriteJson(JsonWriter writer, UserSettings? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

            if (value is not null) WriteValue(writer, value, serializer);

        writer.WriteEndObject();
    }

    static void WriteValue(JsonWriter writer, UserSettings value, JsonSerializer serializer)
    {
        if (value.InstallSoftware.Any())
        {
            writer.WritePropertyName("installsoftware"); writer.WriteStartObject();

                WritePlugins(writer, value.InstallSoftware, serializer);

            writer.WriteEndObject();
        }

        if (value.ThisNodeInstallSoftware.Any())
        {
            writer.WritePropertyName("nodeinstallsoftware"); writer.WriteStartObject();

                writer.WritePropertyName(value.Guid!); writer.WriteStartObject();

                    WritePlugins(writer, value.ThisNodeInstallSoftware, serializer);

                writer.WriteEndObject();

            writer.WriteEndObject();
        }
    }

    static void WritePlugins(JsonWriter writer, IEnumerable<PluginToDeploy> plugins, JsonSerializer serializer)
    {
        foreach (var plugin in plugins.GroupBy(plugin => plugin.Type))
        {
            writer.WritePropertyName(plugin.Key.ToString().ToLowerInvariant()); writer.WriteStartObject();

               foreach (var pluginVersion in plugin) serializer.Serialize(writer, pluginVersion);

            writer.WriteEndObject();
        }
    }
}
