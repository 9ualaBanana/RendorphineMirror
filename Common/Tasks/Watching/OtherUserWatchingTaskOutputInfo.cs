namespace Common.Tasks.Watching;

public class OtherUserWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    public WatchingTaskOutputType Type => WatchingTaskOutputType.OtherNodeTorrent;

    public ITaskOutputInfo CreateOutput(WatchingTask task, string file) => new TorrentTaskOutputInfo();
}
