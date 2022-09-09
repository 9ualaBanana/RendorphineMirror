namespace Node.Tasks.Watching;

public class LocalWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.Local;

    public ITaskOutputInfo CreateOutput(string file) => new TorrentTaskOutputInfo();
}
