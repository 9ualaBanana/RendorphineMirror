namespace Common;

public interface ILoggable
{
    void Log(LogLevel level, string text);
}
public class LoggableLogger : ILoggable
{
    public readonly string? Name;
    public readonly Logger Logger;

    public LoggableLogger(Logger logger) => Logger = logger;
    public LoggableLogger(string? name, Logger logger) : this(logger) => Name = name;

    public void Log(LogLevel level, string text) => Logger.Log(level, Name is null ? text : $"[{Name}] {text}");

    public void Info(string text) => Log(LogLevel.Info, text);
    public void Warn(string text) => Log(LogLevel.Warn, text);
    public void Error(string text) => Log(LogLevel.Error, text);
    public void Error(Exception ex) => Error(ex.ToString());
    public void Trace(string text) => Log(LogLevel.Trace, text);

    public static implicit operator LoggableLogger(Logger logger) => new(logger);
}

public static class LoggableLoggerExtensions
{
    public static LoggableLogger Named(this Logger logger, string? name = null) => new(name, logger);
}