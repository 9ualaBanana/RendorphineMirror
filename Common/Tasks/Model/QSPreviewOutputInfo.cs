namespace Common.Tasks.Model;

public class QSPreviewOutputInfo : ITaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.QSPreview;

    public readonly string Iid;
    public readonly string? TUid;

    public QSPreviewOutputInfo(string iid, string? tuid = null)
    {
        Iid = iid;
        TUid = tuid;
    }
}