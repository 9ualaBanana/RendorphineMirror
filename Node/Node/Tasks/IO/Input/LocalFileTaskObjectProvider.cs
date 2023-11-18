namespace Node.Tasks.IO.Input;

public abstract class LocalFileTaskObjectProvider<TInput> : TaskObjectProvider<TInput>
    where TInput : ITaskInputInfo, ILocalTaskInputInfo
{
    protected static TaskObject GetLocalFileTaskObject(string path)
    {
        path =
            File.Exists(path)
            ? path
            : Directory.GetFiles(path, "*", SearchOption.AllDirectories).First();

        return new TaskObject(Path.GetFileName(path), new FileInfo(path).Length);
    }

    public override Task<OperationResult<TaskObject>> GetTaskObject(TInput input, CancellationToken token) =>
        GetLocalFileTaskObject(input.Path).AsOpResult().AsTask();
}
