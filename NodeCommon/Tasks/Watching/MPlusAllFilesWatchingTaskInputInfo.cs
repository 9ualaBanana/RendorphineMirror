namespace NodeCommon.Tasks.Watching;

public class MPlusAllFilesWatchingTaskInputInfo : IMPlusWatchingTaskInputInfo
{
    public List<string> NonexistentUsers { get; } = new();
    public bool IsUpdatingForNewVersion { get; set; } = false;

    public WatchingTaskInputType Type => WatchingTaskInputType.MPlusAllFiles;
}
