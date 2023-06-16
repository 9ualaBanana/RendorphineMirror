namespace Node.Tasks.Exec;

public record LocalTaskExecutionContext(IReadOnlyCollection<Plugin> Plugins, ILoggable Logger, IMPlusApi? MPlusApi) : ITaskExecutionContext
{
    public void Log(LogLevel level, string text) => Logger.Log(level, text);

    public void SetProgress(double progress) { }
}