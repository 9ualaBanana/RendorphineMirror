using Machine.Plugins;

namespace Node;

public static class Extensions
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static void LogInfo(this ILoggable obj, string text) => _logger.Info($"[{obj.LogName}] {text}");
    public static void LogErr(this ILoggable obj, string text) => _logger.Error($"[{obj.LogName}] {text}");
    public static void LogErr(this ILoggable obj, Exception ex) => obj.LogErr(ex.ToString());


    public static Plugin GetInstance(this PluginType type) => PluginsManager.DiscoverInstalledPlugins().First(x => x.Type == type);
    public static Plugin GetPlugin(this ReceivedTask type) => type.GetAction().Type.GetInstance();
}
