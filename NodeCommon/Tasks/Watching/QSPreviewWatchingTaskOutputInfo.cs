namespace NodeCommon.Tasks.Watching;

public class QSPreviewWatchingTaskOutputInfo : IMPlusWatchingTaskOutputInfo
{
    public WatchingTaskOutputType Type => WatchingTaskOutputType.QSPreview;

    public ITaskOutputInfo CreateOutput(WatchingTask task, string file) => throw new InvalidOperationException("Wrong type yes");
    public ITaskOutputInfo CreateOutput(WatchingTask task, MPlusNewItem item, string file) => new QSPreviewOutputInfo(item.Iid, item.UserId);
}
