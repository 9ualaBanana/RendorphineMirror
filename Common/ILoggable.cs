namespace Common;

public interface ILoggable
{
    string? LogName { get; }
}
public class LoggableLogger : ILoggable
{
    string ILoggable.LogName => Logger.Name;
    public readonly Logger Logger;

    public LoggableLogger(Logger logger) => Logger = logger;

    public static implicit operator LoggableLogger(Logger logger) => new(logger);
}