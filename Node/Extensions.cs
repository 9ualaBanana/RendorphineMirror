using Machine.Plugins;
using Machine.Plugins.Plugins;

namespace Node;

public static class Extensions
{
    public static void LogInfo(this ReceivedTask task, string text) => Log.Information($"[Task {task.Id}] {text}");
    public static void LogErr(this ReceivedTask task, string text) => Log.Error($"[Task {task.Id}] {text}");
    public static void LogErr(this ReceivedTask task, Exception ex) => task.LogErr(ex.ToString());

    public static void LogExecInfo(this ReceivedTask task, string text) => Log.Information($"[TaskExec {task.Id}] {text}");
    public static void LogExecErr(this ReceivedTask task, string text) => Log.Error($"[TaskExec {task.Id}] {text}");

    public static Plugin GetInstance(this PluginType type) => PluginsManager.DiscoverInstalledPlugins().First(x => x.Type == type);
    public static Plugin GetPlugin(this ReceivedTask type) => type.GetAction().Type.GetInstance();
}
