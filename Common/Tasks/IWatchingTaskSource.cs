namespace Common.Tasks;

public interface IWatchingTaskSource : IDisposable
{
    event Action<WatchingTaskFileAddedEventArgs>? FileAdded;

    void StartListening(WatchingTask task);
}
