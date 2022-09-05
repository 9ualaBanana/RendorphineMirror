namespace Common.Tasks.Model;

public class DownloadLinkTaskInputInfo : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.DownloadLink;

    public readonly string Url;

    public DownloadLinkTaskInputInfo(string url) => Url = url;
}
