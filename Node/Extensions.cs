using Machine.Plugins;
using Machine.Plugins.Plugins;

namespace Node;

public static class Extensions
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static void LogInfo(this IHasTaskId task, string text) => _logger.Info($"[Task {task.Id}] {text}");
    public static void LogErr(this IHasTaskId task, string text) => _logger.Error($"[Task {task.Id}] {text}");
    public static void LogErr(this IHasTaskId task, Exception ex) => task.LogErr(ex.ToString());

    public static void LogExecInfo(this IHasTaskId task, string text) => _logger.Info($"[TaskExec {task.Id}] {text}");
    public static void LogExecErr(this IHasTaskId task, string text) => _logger.Error($"[TaskExec {task.Id}] {text}");

    public static void LogInfo(this WatchingTask task, string text) => _logger.Info($"[Watching task {task.Id}] {text}");
    public static void LogErr(this WatchingTask task, string text) => _logger.Error($"[Watching task {task.Id}] {text}");
    public static void LogErr(this WatchingTask task, Exception ex) => task.LogErr(ex.ToString());


    public static Plugin GetInstance(this PluginType type) => PluginsManager.DiscoverInstalledPlugins().First(x => x.Type == type);
    public static Plugin GetPlugin(this ReceivedTask type) => type.GetAction().Type.GetInstance();
}
