using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Machine.Plugins.Deployment;

public class PluginToDeployConverter : JsonConverter<PluginToDeploy>
{
    /// <remarks>
    /// "[plugin_name]": { <br/>
    ///     "[plugin_version]: { <br/>
    ///         "plugins": {[optional_subplugins]} <br/>
    ///     } <br/>
    /// }
    /// </remarks>
    public override PluginToDeploy? ReadJson(JsonReader reader, Type objectType, PluginToDeploy? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var pluginProperty = JProperty.Load(reader);
        // Always takes and deserializes first property (specific plugin version) while they are still left,
        // because the property with that plugin version is removed by higher level deserializer after it's deserialized.
        var pluginVersionProperty = ((JObject)pluginProperty.Value).Properties().FirstOrDefault();
        if (pluginVersionProperty is null) return null;

        return new()
        {
            Type = Enum.Parse<PluginType>(pluginProperty.Name, true),
            Version = pluginVersionProperty.Name,
            SubPlugins = ReadSubpluginsFrom(((JObject)pluginVersionProperty.Value).Property("plugins")!.Value)
        };
    }

    /// <remarks>
    /// "plugins": { <br/>
    ///     "[subplugin_name]": { <br/>
    ///         "version": "[subplugin_version]", <br/>
    ///         "subplugins": {[optional_subsubplugins]} <br/>
    ///     } <br/>
    /// }
    /// </remarks>
    public static IEnumerable<PluginToDeploy>? ReadSubpluginsFrom(JToken pluginVersion)
    {
        return ((JObject)pluginVersion).Properties()
            .Select(subPluginProperty => new PluginToDeploy()
            {
                Type = Enum.Parse<PluginType>(subPluginProperty.Name, true),
                Version = (string) ((JObject)subPluginProperty.Value).Property("version")!,
                SubPlugins = ReadSubSubPluginsFrom(((JObject)subPluginProperty.Value).Property("subplugins")!)
            });
    }

    /// <remarks>
    /// "subplugins": { <br/>
    ///     "[susubplugin_name]": "[subsubplugin_version]", <br/>
    /// }
    /// </remarks>
    static IEnumerable<PluginToDeploy>? ReadSubSubPluginsFrom(JProperty subSubPluginsProperty)
    {
        return ((JObject)subSubPluginsProperty.Value).Properties()
            .Select(subSubPlugin => new PluginToDeploy()
            {
                Type = Enum.Parse<PluginType>(subSubPlugin.Name, true),
                Version = (string)subSubPlugin.Value!
            });
    }

    public override void WriteJson(JsonWriter writer, PluginToDeploy? plugin, JsonSerializer serializer)
    {
        if (plugin is not null) 
            WriteValue(writer, plugin);
    }

    static void WriteValue(JsonWriter writer, PluginToDeploy plugin)
    {
        writer.WritePropertyName(plugin.Version); writer.WriteStartObject();

            writer.WritePropertyName("plugins"); writer.WriteStartObject();

                if (ShouldBeWritten(plugin.SubPlugins))
                    WriteSubplugins(writer, plugin.SubPlugins);

            writer.WriteEndObject();

        writer.WriteEndObject();
    }

    static void WriteSubplugins(JsonWriter writer, IEnumerable<PluginToDeploy> subPlugins)
    {
        foreach (var subPlugin in subPlugins)
            WriteSubplugin(writer, subPlugin);
    }

    static void WriteSubplugin(JsonWriter writer, PluginToDeploy subPlugin)
    {
        writer.WritePropertyName(subPlugin.Type.ToString().ToLowerInvariant()); writer.WriteStartObject();

            writer.WritePropertyName("version"); writer.WriteValue(subPlugin.Version);
            writer.WritePropertyName("subplugins"); writer.WriteStartObject();

                if (ShouldBeWritten(subPlugin.SubPlugins))
                    WriteSubSubPlugins(writer, subPlugin.SubPlugins);

            writer.WriteEndObject();

        writer.WriteEndObject();
    }

    static void WriteSubSubPlugins(JsonWriter writer, IEnumerable<PluginToDeploy> subSubPlugins)
    {
        foreach (var subSubPlugin in subSubPlugins)
            WriteSubSubPlugin(writer, subSubPlugin);
    }

    static void WriteSubSubPlugin(JsonWriter writer, PluginToDeploy subSubPlugin)
    {
        writer.WritePropertyName(subSubPlugin.Type.ToString().ToLowerInvariant());
        writer.WriteValue(subSubPlugin.Version);
    }

    static bool ShouldBeWritten([NotNullWhen(true)] IEnumerable<PluginToDeploy>? childrenPlugins) =>
        childrenPlugins is not null && childrenPlugins.Any();
}
