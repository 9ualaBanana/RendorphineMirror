namespace NodeCommon.Tasks.Watching;

public class Generate3DRFProductTaskInputInfo : IWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.Generate3DRFProduct;

    [LocalDirectory]
    public string InputDirectory { get; }

    [LocalDirectory]
    public string RFProductDirectory { get; }

    public bool AutoCreateRFProducts { get; set; } = false;
    public bool AutoPublishRFProducts { get; set; } = false;

    public Generate3DRFProductTaskInputInfo(string inputDirectory, string rFProductDirectory)
    {
        InputDirectory = inputDirectory;
        RFProductDirectory = rFProductDirectory;
    }
}
