using Newtonsoft.Json;

namespace NodeCommon.Tasks.Model;

public interface ITaskInputOutputInfo
{
    ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
[JsonConverter(typeof(TaskInputJConverter))]
public interface ITaskInputInfo : ITaskInputOutputInfo
{
    TaskInputType Type { get; }
}
public interface ILocalTaskInputInfo : ITaskInputInfo
{
    string Path { get; }

    public ValueTask<OperationResult<TaskObject>> GetTaskObject()
    {
        if (File.Exists(Path)) return get(Path).AsOpResult().AsVTask();
        return get(Directory.GetFiles(Path, "*", SearchOption.AllDirectories).First()).AsOpResult().AsVTask();


        TaskObject get(string file) => new TaskObject(System.IO.Path.GetFileName(file), new FileInfo(file).Length);
    }
}

[JsonConverter(typeof(TaskOutputJConverter))]
public interface ITaskOutputInfo : ITaskInputOutputInfo
{
    TaskOutputType Type { get; }
}