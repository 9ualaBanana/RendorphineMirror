namespace NodeCommon.Tasks.Watching;

public class LocalWatchingTaskInputInfo : IWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.Local;

    [LocalDirectory] public readonly string Directory;
    [Default(0)] public long LastCheck;

    public LocalWatchingTaskInputInfo(string directory) => Directory = directory;
}
