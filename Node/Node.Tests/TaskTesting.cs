namespace Node.Tests;

public record TestContext(IReadOnlyCollection<Plugin> Plugins) : ITaskExecutionContext
{
    public IMPlusApi? MPlusApi => null;
    static readonly Logger Logger = LogManager.GetLogger("Testing");

    public void Log(LogLevel level, string text) => Logger.Log(level, text);
    public void SetProgress(double progress) => Log(LogLevel.Info, $"Task progress: {(int) (progress * 100)}%");
}

public record InstalledPluginsProvider(IReadOnlyCollection<Plugin> Plugins) : IInstalledPluginsProvider;
public record SoftwareListProvider(IReadOnlyDictionary<string, SoftwareDefinition> Software) : ISoftwareListProvider;

public static class TaskTesting
{
    // task itself validates input&output files so we don't need to do much here
    public static async Task TestTaskExecution<T>(PluginManager pluginManager, PluginAction<T> action, T data, ReadOnlyTaskFileList input)
    {
        var plugins = await pluginManager.GetInstalledPluginsAsync();

        var output = new TaskFileListList(TempDirFor(action.Name.ToString()));
        await action.Execute(new TestContext(plugins), new TaskFiles(input, output), data);

        new TestContext(plugins).LogInfo($"{action.GetType().Name} execution completed; Result: [ {string.Join(", ", output.Select(o => $"[{string.Join(", ", o)}]"))}]");
    }

    public static string TempDirFor(string action)
    {
        var dir = Path.GetFullPath("temp/" + action);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);

        return dir;
    }
}
