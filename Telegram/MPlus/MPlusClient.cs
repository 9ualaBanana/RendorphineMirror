using Telegram.Tasks.ResultPreview;

namespace Telegram.MPlus;

public class MPlusClient
{
    internal readonly MPlusTaskManagerClient TaskManager;
    internal readonly MPlusTaskLauncherClient TaskLauncher;

    public MPlusClient(MPlusTaskManagerClient taskManager, MPlusTaskLauncherClient taskLauncher)
    {
        TaskManager = taskManager;
        TaskLauncher = taskLauncher;
    }

    internal async Task<TaskResultFromMPlus> RequestTaskResultAsyncUsing(
        ExecutedTaskApi executedTaskApi,
        MPlusFileAccessor fileAccessor,
        CancellationToken cancellationToken)
        => await RequestTaskResultAsyncUsing(Apis.DefaultWithSessionId(fileAccessor.SessionId), executedTaskApi, fileAccessor, cancellationToken);

    internal async Task<TaskResultFromMPlus> RequestTaskResultAsyncUsing(
        Apis api,
        ExecutedTaskApi executedTaskApi,
        MPlusFileAccessor fileAccessor,
        CancellationToken cancellationToken)
    {
        var fileInfo = await TaskManager.RequestFileInfoAsyncUsing(fileAccessor, cancellationToken);
        var previewDownloadLink = await RequestFileDownloadLinkUsing(api, executedTaskApi, fileAccessor, Extension.jpeg);
        var downloadLink = executedTaskApi.Action is not TaskAction.VeeeVectorize ?
            previewDownloadLink :
            await RequestFileDownloadLinkUsing(api, executedTaskApi, fileAccessor, Extension.eps);

        return TaskResultFromMPlus.Create(executedTaskApi, fileInfo, downloadLink, previewDownloadLink);
    }

    internal async Task<Uri> RequestFileDownloadLinkUsing(Apis api, ExecutedTaskApi executedTaskApi, MPlusFileAccessor fileAccessor, Extension extension)
        => new Uri((await api.GetMPlusItemDownloadLinkAsync(executedTaskApi, fileAccessor.Iid, extension)).ThrowIfError());
}
