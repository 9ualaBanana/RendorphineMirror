namespace NodeCommon.Tasks.Model;

public class DirectDownloadTaskInputInfo : ILocalTaskInputInfo
{
    public TaskInputType Type => TaskInputType.DirectUpload;
    string ILocalTaskInputInfo.Path => Path;

    [LocalFile, NonSerializableForTasks] public string Path;

    public DirectDownloadTaskInputInfo(string? path = null) => Path = path!;
}

public class DirectUploadTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.DirectDownload;
}
