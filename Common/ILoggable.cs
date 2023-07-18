namespace Common;

public interface ILoggable
{
    void Log(LogLevel level, string text);
}