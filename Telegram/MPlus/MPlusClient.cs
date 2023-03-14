using NodeCommon;
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
        var api = Apis.DefaultWithSessionId(fileAccessor.SessionId);
        var mPlusFileInfo = await TaskManager.RequestFileInfoAsyncUsing(fileAccessor, cancellationToken);
        var downloadLink = await RequestFileDownloadLinkUsing(fileAccessor, Extension.jpeg);
        return TaskResultFromMPlus.Create(mPlusFileInfo, taskApi.Executor, downloadLink);
        


        async Task<Uri> RequestFileDownloadLinkUsing(MPlusFileAccessor fileAccessor, Extension extension)
            => new Uri((await api.GetMPlusItemDownloadLinkAsync(taskApi, fileAccessor.Iid, extension)).ThrowIfError());
    }
}
