namespace Common.Tasks.Model;

public class QSPreviewOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.QSPreview;

    public readonly string Iid;
    public readonly string? TUid;

    public QSPreviewOutputInfo(string iid, string? tuid = null)
    {
        Iid = iid;
        TUid = tuid;
    }
}