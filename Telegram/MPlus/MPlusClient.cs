using Telegram.Tasks.ResultPreview;

namespace Telegram.MPlus;

public class MPlusClient
{
    internal readonly MPlusTaskManagerClient TaskManager;

    public MPlusClient(MPlusTaskManagerClient taskManager)
    {
        TaskManager = taskManager;
    }

    internal async Task<TaskResultFromMPlus> RequestTaskResultAsyncUsing(
        ExecutedTaskApi taskApi,
        MPlusFileAccessor fileAccessor,
        CancellationToken cancellationToken)
    {
        var mPlusFileInfo = await TaskManager.RequestFileInfoAsyncUsing(fileAccessor, cancellationToken);
        var downloadLink = await RequestFileDownloadLinkUsing(fileAccessor, Extension.jpeg);
        return TaskResultFromMPlus.Create(mPlusFileInfo, taskApi.Executor, downloadLink);


        async Task<Uri> RequestFileDownloadLinkUsing(MPlusFileAccessor fileAccessor, Extension extension)
            => new Uri((await taskApi.GetMPlusItemDownloadLinkAsync(fileAccessor.Iid, extension, fileAccessor.SessionId)).ThrowIfError());
    }
}
