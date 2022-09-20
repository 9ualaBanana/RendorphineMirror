using System.Net;

namespace Node.Tasks.Handlers;

public class DownloadLinkTaskHandler : ITaskInputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.DownloadLink;

    public async ValueTask Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (DownloadLinkTaskInputInfo) task.Input;

        using var data = await Api.Get(info.Url);
        if (info.Url.Contains("t.microstock.plus") && data.StatusCode == HttpStatusCode.NotFound)
            task.ThrowCancel("Got 404 when trying to get image from the reepo");

        if (data.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException($"Download link `{info.Url}` request returned status code {data.StatusCode}", null, data.StatusCode);

        using (var file = File.Open(task.FSNewInputFile(), FileMode.Create, FileAccess.Write))
            await data.Content.CopyToAsync(file, cancellationToken);
    }

}
