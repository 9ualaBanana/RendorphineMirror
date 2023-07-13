namespace Common;

public class NamedLogger : ILoggable
{
    readonly string? Name;
    readonly ILoggable Logger;

    public NamedLogger(string name, ILoggable logger)
    {
        Name = name;
        Logger = logger;
    }

    public void Log(LogLevel level, string text) => Logger.Log(level, $"[{Name}] {text}");

    public static NamedLogger? TryFrom(string name, ILoggable? logger) => logger is null ? null : new NamedLogger(name, logger);
}
