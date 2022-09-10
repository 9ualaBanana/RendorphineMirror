namespace Common.Tasks.Watching;

public interface IWatchingTaskInputOutputInfo
{
    WatchingTaskInputOutputType Type { get; }
}
public interface IWatchingTaskSource : IWatchingTaskInputOutputInfo, IDisposable
{
    void StartListening(WatchingTask task);
}
public interface IWatchingTaskOutputInfo : IWatchingTaskInputOutputInfo
{
    ITaskOutputInfo CreateOutput(string file);
}