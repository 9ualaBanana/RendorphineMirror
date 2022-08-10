namespace Node.Tasks.Watching;

// TODO:
public class MPlusWatchingTaskSource : IWatchingTaskSource
{
    public event Action<WatchingTaskFileAddedEventArgs>? FileAdded;

    public void StartListening(WatchingTask task)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
