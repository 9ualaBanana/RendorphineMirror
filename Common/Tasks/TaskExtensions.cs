namespace Common.Tasks;

public static class TaskExtensions
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static void LogInfo(this ILoggable obj, string text) => _logger.Info($"[{obj.LogName}] {text}");
    public static void LogErr(this ILoggable obj, string text) => _logger.Error($"[{obj.LogName}] {text}");
    public static void LogErr(this ILoggable obj, Exception ex) => obj.LogErr(ex.ToString());


    public static Plugin GetInstance(this PluginType type) => NodeGlobalState.Instance.GetPluginInstance(type);
}
