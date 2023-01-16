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

    ValueTask<TaskObject> GetFileInfo();
}
[JsonConverter(typeof(TaskOutputJConverter))]
public interface ITaskOutputInfo : ITaskInputOutputInfo
{
    TaskOutputType Type { get; }
}