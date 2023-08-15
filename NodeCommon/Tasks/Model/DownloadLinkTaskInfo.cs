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
}
