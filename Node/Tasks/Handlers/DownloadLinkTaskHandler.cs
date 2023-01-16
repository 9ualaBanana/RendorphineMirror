using System.Net;

namespace Node.Tasks.Handlers;

public class DownloadLinkTaskHandler : ITaskInputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.DownloadLink;

    public async ValueTask Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (DownloadLinkTaskInputInfo) task.Input;

        using var data = await Api.Default.Get(info.Url);
        if (info.Url.Contains("t.microstock.plus") && data.StatusCode == HttpStatusCode.NotFound)
            task.ThrowFailed("Got 404 when trying to get image from the reepo");

        if (data.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException($"Download link `{info.Url}` request returned status code {data.StatusCode}", null, data.StatusCode);

        var format = tryall(
            () => FileFormatExtensions.FromMime(data.Content.Headers.ContentType!.ToString()),
            () => FileFormatExtensions.FromFilename(data.Content.Headers.ContentDisposition!.FileName!),
            () => FileFormatExtensions.FromFilename(info.Url.Split('?')[0]),
            () => task.GetAction().InputRequirements.Single(x => x.Required).Format
        );

        using (var file = File.Open(task.FSNewInputFile(format), FileMode.Create, FileAccess.Write))
            await data.Content.CopyToAsync(file, cancellationToken);


        FileFormat tryall(params Func<FileFormat>[] funcs)
        {
            for (int i = 0; i < funcs.Length; i++)
            {
                try { return funcs[i](); }
                catch
                {
                    if (i == funcs.Length - 1)
                        throw;
                }
            }

            throw new Exception("Could not find a valid file format");
        }
    }
}
