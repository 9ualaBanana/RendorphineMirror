namespace Node.Tests;

public record InstalledPluginsProvider(IReadOnlyCollection<Plugin> Plugins) : IInstalledPluginsProvider;
public record SoftwareListProvider(IReadOnlyDictionary<string, SoftwareDefinition> Software) : ISoftwareListProvider;

public static class TaskTesting
{
    public static string TempDirFor(string action)
    {
        var dir = Path.GetFullPath("temp/" + action);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);

        return dir;
    }
}
