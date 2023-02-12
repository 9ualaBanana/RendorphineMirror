namespace NodeCommon.Tasks.Watching;

public class MPlusWatchingTaskInputInfo : IMPlusWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.MPlus;

    [MPlusDirectory] public readonly string Directory;
    public string? SinceIid { get; set; }

    public MPlusWatchingTaskInputInfo(string directory) => Directory = directory;
}
