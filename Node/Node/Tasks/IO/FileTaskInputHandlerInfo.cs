namespace Node.Tasks.IO;

public abstract class FileTaskInputHandlerInfo<TData> : TaskInputHandlerInfo<TData, ReadOnlyTaskFileList> where TData : ITaskInputInfo
{
    protected static TaskObject GetLocalFileTaskObject(string path)
    {
        if (File.Exists(path)) return get(path);
        return get(Directory.GetFiles(path, "*", SearchOption.AllDirectories).First());


        static TaskObject get(string file) => new TaskObject(System.IO.Path.GetFileName(file), new FileInfo(file).Length);
    }


    protected abstract class FileHandlerBase : HandlerBase
    {
        public required ITaskInputDirectoryProvider TaskDirectoryProvider { get; init; }
    }
}
