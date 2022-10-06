namespace Common.Tasks.Watching;

public class MPlusAllFilesWatchingTaskInputInfo : IMPlusWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.MPlusAllFiles;

    [Default(false)] public bool SkipWatermarked { get; init; }
    public string? SinceIid { get; set; }
}
