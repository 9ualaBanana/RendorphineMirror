using Newtonsoft.Json;

namespace Node.Plugins.Deployment;

[JsonConverter(typeof(PluginToDeployConverter))]
public class PluginToDeploy : IEquatable<PluginToDeploy>, IEquatable<Plugin>
{
    public PluginType Type { get; set; }
    public string Version { get; set; } = null!;
    public HashSet<PluginToDeploy> SubPlugins { get; set; } = new();
    internal IEnumerable<PluginToDeploy> AsEnumerable => new PluginToDeploy[] { this };
    public IEnumerable<PluginToDeploy> SelfAndSubPlugins => SubPlugins.Any() ?
        AsEnumerable.Concat(SubPlugins.SelectMany(subPlugin => subPlugin.SelfAndSubPlugins)) : AsEnumerable;

    public PluginDeploymentInfo GetDeploymentInfo(string? installationPath = default) => Type switch
    {
        PluginType.Blender => new BlenderDeploymentInfo(installationPath),
        PluginType.Python => new PythonDeploymentInfo(installationPath),
        _ => new ScriptPluginDeploymentInfo(this),
    };


    #region EqualityContract
    public static bool operator !=(PluginToDeploy this_, PluginToDeploy? other) => !this_.Equals(other);
    public static bool operator ==(PluginToDeploy this_, PluginToDeploy? other) => this_.Equals(other);
    public override bool Equals(object? obj) => Equals(obj as PluginToDeploy);
    public bool Equals(PluginToDeploy? other) => Type == other?.Type/* && Version == other?.Version*/;

    public static bool operator !=(PluginToDeploy this_, Plugin? other) => !this_.Equals(other);
    public static bool operator ==(PluginToDeploy this_, Plugin? other) => this_.Equals(other);
    public bool Equals(Plugin? other) => Type == other?.Type/* && Version == other?.Version*/;
    public override int GetHashCode() => Type.GetHashCode()/* ^ Version.GetHashCode()*/;
    #endregion
}

public static class PluginToDeplooyExtensions
{
    /// <summary>
    /// Performs union between plugins hierarchies for each plugin from both sets using default comparer.
    /// </summary>
    public static void UnionEachWith(this HashSet<PluginToDeploy> thesePlugins, HashSet<PluginToDeploy> otherPlugins)
    {
        thesePlugins.UnionWith(otherPlugins);
        foreach (var thisPlugin in thesePlugins)
        {
            var otherPlugin = otherPlugins.SingleOrDefault(otherPlugin => thisPlugin == otherPlugin);
            if (otherPlugin is not null) thisPlugin.SubPlugins.UnionEachWith(otherPlugin.SubPlugins);
        }
    }
}
