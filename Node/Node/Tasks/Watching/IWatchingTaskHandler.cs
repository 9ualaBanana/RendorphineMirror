namespace Node.Tasks.Watching;

public interface IWatchingTaskHandler { }
public interface IWatchingTaskInputHandler : IWatchingTaskHandler, IDisposable
{
    WatchingTaskInputType Type { get; }
    WatchingTask Task { get; }

    void StartListening();
}