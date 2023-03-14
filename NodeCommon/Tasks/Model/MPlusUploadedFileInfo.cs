namespace NodeCommon.Tasks.Model;

public record MPlusUploadedFileInfo(string Iid, string FileName) : IUploadedFileInfo
{
    public TaskOutputType Type => TaskOutputType.MPlus;
}
