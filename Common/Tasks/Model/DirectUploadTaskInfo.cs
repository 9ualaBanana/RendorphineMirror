namespace Common.Tasks.Model;

public class DirectDownloadTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.DirectUpload;

    public readonly string Host;
    public readonly int Port;
    [LocalFile] public readonly string Path;
    [Hidden] public bool Downloaded = false;

    public DirectDownloadTaskInputInfo(string host, int port, string? path = null)
    {
        Host = host;
        Port = port;
        Path = path!;
    }
}

public class DirectUploadTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.DirectDownload;

    public readonly string Host;
    public readonly int Port;

    public DirectUploadTaskOutputInfo(string host, int port)
    {
        Host = host;
        Port = port;
    }
}
