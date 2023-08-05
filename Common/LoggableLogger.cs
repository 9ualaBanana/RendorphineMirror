namespace Common;

public class LoggableLogger : ILoggable
{
    readonly string? Name;
    readonly Logger Logger;

    public LoggableLogger(Logger logger) => Logger = logger;
    public LoggableLogger(string? name, Logger logger) : this(logger) => Name = name;

    public void Log(LogLevel level, string text) => Logger.Log(level, Name is null ? text : $"[{Name}] {text}");

    public static implicit operator LoggableLogger(Logger logger) => new(logger);
}