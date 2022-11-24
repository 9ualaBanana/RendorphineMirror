namespace NodeCommon.Tasks.Model;

public class DirectDownloadTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.DirectUpload;

    [LocalFile, NonSerializableForTasks] public string Path;
    [Hidden, NonSerializableForTasks] public bool Downloaded = false;

    public DirectDownloadTaskInputInfo(string? path = null) => Path = path!;

    public ValueTask<TaskObject> GetFileInfo()
    {
        if (File.Exists(Path)) return get(Path).AsVTask();
        return get(Directory.GetFiles(Path, "*", SearchOption.AllDirectories).First()).AsVTask();


        TaskObject get(string file) => new TaskObject(System.IO.Path.GetFileName(file), new FileInfo(file).Length);
    }
}

public class DirectUploadTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.DirectDownload;
}
