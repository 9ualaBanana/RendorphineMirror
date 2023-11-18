namespace Common;

public interface ILoggable
{
    void Log(LogLevel level, string text);

    void Log(Microsoft.Extensions.Logging.LogLevel level, string text) =>
        Log(level switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => LogLevel.Trace,
            Microsoft.Extensions.Logging.LogLevel.Debug => LogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => LogLevel.Info,
            Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.Warn,
            Microsoft.Extensions.Logging.LogLevel.Error => LogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => LogLevel.Fatal,
            Microsoft.Extensions.Logging.LogLevel.None => LogLevel.Off,
            _ => throw new InvalidOperationException(),
        }, text);
}