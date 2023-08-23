namespace NodeCommon.Tasks.Watching;

public class MPlusAllFilesWatchingTaskInputInfo : IMPlusWatchingTaskInputInfo
{
    public List<string> NonexistentUsers { get; } = new();

    public WatchingTaskInputType Type => WatchingTaskInputType.MPlusAllFiles;
}
