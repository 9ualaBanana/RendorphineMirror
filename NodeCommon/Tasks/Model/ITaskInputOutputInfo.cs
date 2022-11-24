namespace NodeCommon.Tasks.Model;

public interface ITaskInputOutputInfo
{
    ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
public interface ITaskInputInfo : ITaskInputOutputInfo
{
    TaskInputType Type { get; }

    ValueTask<TaskObject> GetFileInfo();
}
public interface ITaskOutputInfo : ITaskInputOutputInfo
{
    TaskOutputType Type { get; }
}