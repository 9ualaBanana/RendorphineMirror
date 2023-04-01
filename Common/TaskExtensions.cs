namespace Common;

public static class LogExtensions
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static void Log(this ILoggable obj, LogLevel level, string text) =>
        ((obj as LoggableLogger)?.Logger ?? _logger).Log(level, (obj.LogName is null ? null : $"[{obj.LogName}]") + text);

    public static void LogInfo(this ILoggable obj, string text) => obj.Log(LogLevel.Info, text);
    public static void LogWarn(this ILoggable obj, string text) => obj.Log(LogLevel.Warn, text);
    public static void LogErr(this ILoggable obj, string text) => obj.Log(LogLevel.Error, text);
    public static void LogErr(this ILoggable obj, Exception ex) => obj.LogErr(ex.ToString());
    public static void LogTrace(this ILoggable obj, string text) => obj.Log(LogLevel.Trace, text);

    public static LoggableLogger AsLoggable(this Logger logger) => new(logger);
}
