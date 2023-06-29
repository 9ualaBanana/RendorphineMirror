namespace Node.Plugins;

public interface IInstalledPluginsProvider
{
    IReadOnlyCollection<Plugin> Plugins { get; }
}
