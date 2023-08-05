namespace NodeCommon.Tasks.Watching;

public record MPlusNewItem(string Iid, string UserId, MPlusNewItemFiles Files, long Registered);
public record MPlusNewItemFiles(MPlusNewItemFile Jpeg, MPlusNewItemFile? Mov = null)
{
    public MPlusNewItemFile File => Mov ?? Jpeg;
}

public record MPlusNewItemFile(string FileName, long Size);
public record MPlusNewItemQSPreviewBase(ulong Size, string Url);
public record MPlusNewItemQSPreview(ulong Size, string Url, MPlusNewItemQSPreviewBase? Mp4 = null) : MPlusNewItemQSPreviewBase(Size, Url);