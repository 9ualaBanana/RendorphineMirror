using Newtonsoft.Json;

namespace NodeCommon.Tasks.Model;

public class DownloadLinkTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.DownloadLink;

    public readonly string Url;

    public DownloadLinkTaskInputInfo(Uri url)
        : this(url.ToString())
    {
    }

    [JsonConstructor]
    public DownloadLinkTaskInputInfo(string url) => Url = url;

    public async ValueTask<TaskObject> GetFileInfo()
    {
        var headers = await Api.Client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);
        return new TaskObject(Path.GetFileName(Url), headers.Content.Headers.ContentLength!.Value);
    }
}
