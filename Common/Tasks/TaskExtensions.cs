namespace Common.Tasks;

public static class TaskExtensions
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static void Log(this ILoggable obj, LogLevel level, string text) => _logger.Log(level, $"[{obj.LogName}] {text}");
    public static void LogInfo(this ILoggable obj, string text) => _logger.Info($"[{obj.LogName}] {text}");
    public static void LogWarn(this ILoggable obj, string text) => _logger.Warn($"[{obj.LogName}] {text}");
    public static void LogErr(this ILoggable obj, string text) => _logger.Error($"[{obj.LogName}] {text}");
    public static void LogErr(this ILoggable obj, Exception ex) => obj.LogErr(ex.ToString());
    public static void LogTrace(this ILoggable obj, string text) => _logger.Trace($"[{obj.LogName}] {text}");
}
