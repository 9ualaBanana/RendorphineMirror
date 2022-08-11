namespace Node.Tasks.Watching;

public interface IWatchingTaskSource : IDisposable
{
    event Action<WatchingTaskFileAddedEventArgs>? FileAdded;

    void StartListening(WatchingTask task);
}
