using Newtonsoft.Json;

namespace Machine.Plugins.Deployment;

[JsonConverter(typeof(PluginToDeployConverter))]
public class PluginToDeploy : IEquatable<PluginToDeploy>, IEquatable<Plugin>
{
    public PluginType Type { get; set; }
    public string Version { get; set; } = null!;
    public IEnumerable<PluginToDeploy>? SubPlugins { get; set; }

    public IEnumerable<PluginToDeploy> SelfAndSubPlugins
    {
        get
        {
            var selfAsEnumerable = new PluginToDeploy[] { this };
            return SubPlugins is null ? selfAsEnumerable
                : selfAsEnumerable.Concat(SubPlugins.SelectMany(subPlugin => subPlugin.SelfAndSubPlugins));
        }
    }

    public PluginDeploymentInfo GetDeploymentInfo(string? installationPath = default) => Type switch
    {
        PluginType.Blender => new BlenderDeploymentInfo(installationPath),
        PluginType.Python => new PythonDeploymentInfo(installationPath),
        PluginType.Python_Esrgan => new PythonEsrganDeploymentInfo(installationPath),
    };


    #region EqualityContract
    public static bool operator !=(PluginToDeploy this_, Plugin? other) => !this_.Equals(other);
    public static bool operator ==(PluginToDeploy this_, Plugin? other) => this_.Equals(other);

    public override bool Equals(object? obj) => Equals(obj as PluginToDeploy);
    public bool Equals(PluginToDeploy? other) => Type == other?.Type/* && Version == other?.Version*/;
    public bool Equals(Plugin? other) => Type == other?.Type/* && Version == other?.Version*/;
    public override int GetHashCode() => Type.GetHashCode()/* ^ Version.GetHashCode()*/;
    #endregion
}
