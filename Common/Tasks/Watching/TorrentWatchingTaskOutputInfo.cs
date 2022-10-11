namespace Common.Tasks.Watching;

public class TorrentWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    public WatchingTaskOutputType Type => WatchingTaskOutputType.Torrent;

    public ITaskOutputInfo CreateOutput(WatchingTask task, string file) => new TorrentTaskOutputInfo();
}
