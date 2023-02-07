namespace NodeCommon.Tasks.Model;

public record MPlusUploadedFileInfo(string Iid) : IUploadedFileInfo
{
    public TaskOutputType Type => TaskOutputType.MPlus;
}
