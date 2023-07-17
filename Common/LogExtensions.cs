namespace Common;

public static class LogExtensions
{
    public static void LogInfo(this ILoggable loggable, string text) => loggable.Log(LogLevel.Info, text);
    public static void LogWarn(this ILoggable loggable, string text) => loggable.Log(LogLevel.Warn, text);
    public static void LogErr(this ILoggable loggable, string text) => loggable.Log(LogLevel.Error, text);
    public static void LogErr(this ILoggable loggable, Exception ex) => loggable.LogErr(ex.ToString());
    public static void LogTrace(this ILoggable loggable, string text) => loggable.Log(LogLevel.Trace, text);

    public static LoggableLogger AsLoggable(this Logger logger) => new(logger);

    // compatibility with NLog.ILogger
    public static void Info(this ILoggable loggable, string text) => loggable.Log(LogLevel.Info, text);
    public static void Warning(this ILoggable loggable, string text) => loggable.Log(LogLevel.Warn, text);
    public static void Error(this ILoggable loggable, string text) => loggable.Log(LogLevel.Error, text);
    public static void Error(this ILoggable loggable, Exception ex) => loggable.LogErr(ex.ToString());
    public static void Trace(this ILoggable loggable, string text) => loggable.Log(LogLevel.Trace, text);
}