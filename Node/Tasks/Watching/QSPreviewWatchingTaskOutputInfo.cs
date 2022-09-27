namespace Node.Tasks.Watching;

public class QSPreviewWatchingTaskOutputInfo : IMPlusWatchingTaskOutputInfo
{
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.QSPreview;

    public ITaskOutputInfo CreateOutput(string file) => throw new InvalidOperationException("Wrong type yes");
    public ITaskOutputInfo CreateOutput(MPlusNewItem item, string file) => new QSPreviewOutputInfo(item.Iid, item.UserId);
}
