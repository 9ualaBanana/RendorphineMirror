namespace NodeCommon.Tasks.Model;

public class DirectUploadTaskInputInfo : ILocalTaskInputInfo
{
    public TaskInputType Type => TaskInputType.DirectUpload;
    string ILocalTaskInputInfo.Path => Path;

    [LocalFile, NonSerializableForTasks] public string Path;

    public DirectUploadTaskInputInfo(string? path = null) => Path = path!;
}

public class DirectDownloadTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.DirectDownload;
}
