namespace Common.Tasks.Watching;

public class LocalWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    public WatchingTaskOutputType Type => WatchingTaskOutputType.Local;

    public ITaskOutputInfo CreateOutput(WatchingTask task, string file) => new TorrentTaskOutputInfo();
}
