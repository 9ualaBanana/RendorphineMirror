namespace Common;

public static class LogExtensions
{
    public static void LogInfo(this ILoggable loggable, string text) => loggable.Log(LogLevel.Info, text);
    public static void LogWarn(this ILoggable loggable, string text) => loggable.Log(LogLevel.Warn, text);
    public static void LogErr(this ILoggable loggable, string text) => loggable.Log(LogLevel.Error, text);
    public static void LogErr(this ILoggable loggable, Exception ex) => loggable.LogErr(ex.ToString());
    public static void LogTrace(this ILoggable loggable, string text) => loggable.Log(LogLevel.Trace, text);
}