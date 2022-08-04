using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Machine.Plugins.Deployment;

public class PluginToDeployConverter : JsonConverter<PluginToDeploy>
{
    public override PluginToDeploy? ReadJson(JsonReader reader, Type objectType, PluginToDeploy? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var pluginProperty = JProperty.Load(reader);
        var pluginVersionProperty = ((JObject)pluginProperty.Value).Properties().FirstOrDefault();
        if (pluginVersionProperty is null) return null;

        return new()
        {
            Type = Enum.Parse<PluginType>(pluginProperty.Name, true),
            Version = pluginVersionProperty.Name,
            SubPlugins = ReadSubpluginsFrom(pluginVersionProperty)
        };
    }

    public static IEnumerable<PluginToDeploy>? ReadSubpluginsFrom(JProperty pluginVersion)
    {
        var subPluginsInfo = new List<PluginToDeploy>();

        var subPlugins = ((JObject)pluginVersion.Value).Property("plugins")!.Value;
        foreach (var subPluginInfoProperty in ((JObject)subPlugins).Properties())
        {
            var subPluginInfoObject = (JObject)subPluginInfoProperty.Value;
            var subPluginInfo = new PluginToDeploy()
            {
                Type = Enum.Parse<PluginType>(subPluginInfoProperty.Name, true),
                Version = (string)subPluginInfoObject.Property("version")!,
                SubPlugins = ReadSubSubPluginsFrom(subPluginInfoObject.Property("subplugins")!)
            };
            subPluginsInfo.Add(subPluginInfo);
        }

        return subPluginsInfo;
    }

    static IEnumerable<PluginToDeploy>? ReadSubSubPluginsFrom(JProperty subSubPlugins)
    {
        return ((JObject)subSubPlugins.Value).Properties().Select(ssp => new PluginToDeploy()
        {
            Type = Enum.Parse<PluginType>(ssp.Name, true),
            Version = (string)ssp.Value!
        });
    }

    public override void WriteJson(JsonWriter writer, PluginToDeploy? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        if (value is not null) WriteValue(writer, value);
        writer.WriteEndObject();
    }

    static void WriteValue(JsonWriter writer, PluginToDeploy value)
    {
        writer.WritePropertyName(value.Version);
        writer.WriteStartObject();
        writer.WritePropertyName("plugins");
        writer.WriteStartObject();
        if (value.SubPlugins is not null)
            WriteSubplugins(writer, value.SubPlugins);
        writer.WriteEndObject();
    }

    static void WriteSubplugins(JsonWriter writer, IEnumerable<PluginToDeploy> subPlugins)
    {
        foreach (var subPlugin in subPlugins)
            WriteSubplugin(writer, subPlugin);
    }

    static void WriteSubplugin(JsonWriter writer, PluginToDeploy subPlugin)
    {
        writer.WritePropertyName(subPlugin.Type.ToString());
        writer.WriteStartObject();
        writer.WritePropertyName("version");
        writer.WriteValue(subPlugin.Version);
        writer.WritePropertyName("subplugins");
        writer.WriteStartObject();
        if (subPlugin.SubPlugins is not null)
            WriteSubSubPlugins(writer, subPlugin.SubPlugins);
        writer.WriteEndObject();
    }

    static void WriteSubSubPlugins(JsonWriter writer, IEnumerable<PluginToDeploy> subSubPlugins)
    {
        foreach (var subSubPlugin in subSubPlugins)
        {
            writer.WritePropertyName(subSubPlugin.Type.ToString());
            writer.WriteValue(subSubPlugin.Version);
        }
    }
}
