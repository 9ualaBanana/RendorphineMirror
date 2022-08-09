using Machine.Plugins;
using Machine.Plugins.Plugins;

namespace Node;

public static class Extensions
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static void LogInfo(this ReceivedTask task, string text) => _logger.Info("[Task {Id}] {Text}", task.Id, text);
    public static void LogErr(this ReceivedTask task, string text) => _logger.Error("[Task {Id}] {Text}", task.Id, text);
    public static void LogErr(this ReceivedTask task, Exception ex) => task.LogErr(ex.ToString());

    public static void LogExecInfo(this ReceivedTask task, string text) => _logger.Info("[TaskExec {Id}] {Text}", task.Id, text);
    public static void LogExecErr(this ReceivedTask task, string text) => _logger.Error("[TaskExec {Id}] {Text}", task.Id, text);

    public static Plugin GetInstance(this PluginType type) => PluginsManager.DiscoverInstalledPlugins().First(x => x.Type == type);
    public static Plugin GetPlugin(this ReceivedTask type) => type.GetAction().Type.GetInstance();
}
