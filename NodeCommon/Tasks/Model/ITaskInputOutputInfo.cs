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
}

[JsonConverter(typeof(TaskOutputJConverter))]
public interface ITaskOutputInfo : ITaskInputOutputInfo
{
    TaskOutputType Type { get; }
}