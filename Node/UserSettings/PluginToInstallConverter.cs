using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.UserSettings;

public class PluginToInstallConverter : JsonConverter<PluginToInstall>
{
    public override PluginToInstall? ReadJson(JsonReader reader, Type objectType, PluginToInstall? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var pluginProperty = JProperty.Load(reader);
        var pluginVersionProperty = ((JObject)pluginProperty.Value).Properties().FirstOrDefault();
        if (pluginVersionProperty is null) return null;

        return new()
        {
            Type = pluginProperty.Name,
            Version = pluginVersionProperty.Name,
            SubPlugins = ReadSubpluginsFrom(pluginVersionProperty)
        };
    }

    public static IEnumerable<PluginToInstall>? ReadSubpluginsFrom(JProperty pluginVersion)
    {
        var subPluginsInfo = new List<PluginToInstall>();

        var subPlugins = ((JObject)pluginVersion.Value).Property("plugins")!.Value;
        foreach (var subPluginInfoProperty in ((JObject)subPlugins).Properties())
        {
            var subPluginInfoObject = (JObject)subPluginInfoProperty.Value;
            var subPluginInfo = new PluginToInstall()
            {
                Type = subPluginInfoProperty.Name,
                Version = (string)subPluginInfoObject.Property("version")!,
                SubPlugins = ReadSubSubPluginsFrom(subPluginInfoObject.Property("subplugins")!)
            };
            subPluginsInfo.Add(subPluginInfo);
        }

        return subPluginsInfo;
    }

    static IEnumerable<PluginToInstall>? ReadSubSubPluginsFrom(JProperty subSubPlugins)
    {
        return ((JObject)subSubPlugins.Value).Properties().Select(ssp => new PluginToInstall()
        {
            Type = ssp.Name,
            Version = (string)ssp.Value!
        });
    }

    public override void WriteJson(JsonWriter writer, PluginToInstall? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        if (value is not null) WriteValue(writer, value);
        writer.WriteEndObject();
    }

    static void WriteValue(JsonWriter writer, PluginToInstall value)
    {
        writer.WritePropertyName(value.Version);
        writer.WriteStartObject();
        writer.WritePropertyName("plugins");
        writer.WriteStartObject();
        if (value.SubPlugins is not null)
            WriteSubplugins(writer, value.SubPlugins);
        writer.WriteEndObject();
    }

    static void WriteSubplugins(JsonWriter writer, IEnumerable<PluginToInstall> subPlugins)
    {
        foreach (var subPlugin in subPlugins)
            WriteSubplugin(writer, subPlugin);
    }
    
    static void WriteSubplugin(JsonWriter writer, PluginToInstall subPlugin)
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
