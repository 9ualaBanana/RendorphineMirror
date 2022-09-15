namespace Node.Tasks.Handlers;

public class DownloadLinkTaskHandler : ITaskInputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.DownloadLink;

    public async ValueTask Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (DownloadLinkTaskInputInfo) task.Input;

        using (var inputStream = await Api.Download(info.Url))
        using (var file = File.Open(task.FSNewInputFile(), FileMode.Create, FileAccess.Write))
            await inputStream.CopyToAsync(file, cancellationToken);
    }

}
