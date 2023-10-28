namespace NodeCommon.Tasks.Watching;

public class OneClickWatchingTaskInputInfo : IWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.OneClick;

    [LocalDirectory]
    public string InputDirectory { get; }

    [LocalDirectory]
    public string OutputDirectory { get; }

    [LocalDirectory]
    public string LogDirectory { get; }

    public OneClickWatchingTaskInputInfo(string inputDirectory, string outputDirectory, string logDirectory)
    {
        InputDirectory = inputDirectory;
        OutputDirectory = outputDirectory;
        LogDirectory = logDirectory;
    }
}
