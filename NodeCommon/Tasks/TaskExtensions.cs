namespace NodeCommon.Tasks;

public static class TaskExtensions
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static void Log(this ILoggable obj, LogLevel level, string text) =>
        ((obj as LoggableLogger)?.Logger ?? _logger).Log(level, $"[{obj.LogName}] {text}");

    public static void LogInfo(this ILoggable obj, string text) => _logger.Log(LogLevel.Info, $"[{obj.LogName}] {text}");
    public static void LogWarn(this ILoggable obj, string text) => _logger.Log(LogLevel.Warn, $"[{obj.LogName}] {text}");
    public static void LogErr(this ILoggable obj, string text) => _logger.Log(LogLevel.Error, $"[{obj.LogName}] {text}");
    public static void LogErr(this ILoggable obj, Exception ex) => obj.LogErr(ex.ToString());
    public static void LogTrace(this ILoggable obj, string text) => _logger.Log(LogLevel.Trace, $"[{obj.LogName}] {text}");

    public static LoggableLogger AsLoggable(this Logger logger) => new(logger);
}
