namespace NodeCommon.Tasks.Watching;

public record DirectoryStructurePart(DateTimeOffset LastChanged, long? Size);
public class Generate3DRFProductTaskInputInfo : IWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.Generate3DRFProduct;

    [LocalDirectory]
    public string InputDirectory { get; }

    [Hidden]
    public Dictionary<string, Dictionary<string, DirectoryStructurePart>>? DirectoryStructure { get; set; }

    [Hidden]
    public DateTimeOffset LastSalesFetch { get; set; } = DateTimeOffset.MinValue;

    public Generate3DRFProductTaskInputInfo(string inputDirectory) => InputDirectory = inputDirectory;
}
