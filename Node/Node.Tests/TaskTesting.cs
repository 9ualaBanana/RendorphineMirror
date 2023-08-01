namespace Node.Tests;

public record TestContext(IReadOnlyCollection<Plugin> Plugins) : ITaskExecutionContext
{
    public IMPlusApi? MPlusApi => null;
    static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("Testing");

    public void Log(NLog.LogLevel level, string text) => Logger.Log(level, text);
    public void SetProgress(double progress) => Log(NLog.LogLevel.Info, $"Task progress: {(int) (progress * 100)}%");
}

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
