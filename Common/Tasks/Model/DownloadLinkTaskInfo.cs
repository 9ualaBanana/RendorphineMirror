namespace Common.Tasks.Model;

public class DownloadLinkTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.DownloadLink;

    public readonly string Url;

    public DownloadLinkTaskInputInfo(string url) => Url = url;
}
