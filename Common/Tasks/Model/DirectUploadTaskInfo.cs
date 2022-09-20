namespace Common.Tasks.Model;

public class DirectDownloadTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.DirectUpload;

    [LocalFile] public readonly string Path;
    [Hidden] public bool Downloaded = false;

    public DirectDownloadTaskInputInfo(string? path = null) => Path = path!;
}

public class DirectUploadTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.DirectDownload;
}
