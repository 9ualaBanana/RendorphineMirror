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
        ExecutedTaskApi executedTask,
        MPlusFileAccessor fileAccessor,
        CancellationToken cancellationToken)
    {
        var api = Apis.DefaultWithSessionId(fileAccessor.SessionId);
        var mPlusFileInfo = await TaskManager.RequestFileInfoAsyncUsing(fileAccessor, cancellationToken);
        var downloadLink = await RequestFileDownloadLinkUsing(fileAccessor, executedTask.Action is TaskAction.VeeeVectorize ? Extension.eps : Extension.jpeg);
        return TaskResultFromMPlus.Create(mPlusFileInfo, executedTask.Action, executedTask.Executor, downloadLink);
        


        async Task<Uri> RequestFileDownloadLinkUsing(MPlusFileAccessor fileAccessor, Extension extension)
            => new Uri((await api.GetMPlusItemDownloadLinkAsync(executedTask, fileAccessor.Iid, extension)).ThrowIfError());
    }
}
