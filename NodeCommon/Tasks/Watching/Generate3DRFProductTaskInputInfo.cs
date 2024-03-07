namespace NodeCommon.Tasks.Watching;

public record DirectoryStructureHolder(bool NeedsUploading, Dictionary<string, DirectoryStructurePart> Parts);
public record DirectoryStructurePart(DateTimeOffset LastChanged, long? Size);
public class Generate3DRFProductTaskInputInfo : IWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.Generate3DRFProduct;

    [LocalDirectory]
    public string InputDirectory { get; }

    [Hidden]
    public Dictionary<string, DirectoryStructureHolder>? DirectoryStructure2 { get; set; }

    [Hidden]
    public DateTimeOffset LastSalesFetch { get; set; } = DateTimeOffset.MinValue;

    public Generate3DRFProductTaskInputInfo(string inputDirectory) => InputDirectory = inputDirectory;
}
