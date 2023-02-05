namespace NodeCommon.Tasks.Model;

public class QSPreviewOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.QSPreview;

    public readonly string Iid;
    public readonly string? TUid;
    [Hidden] public string? IngesterHost;

    public QSPreviewOutputInfo(string iid, string? tuid = null)
    {
        Iid = iid;
        TUid = tuid;
    }
}