namespace Common.Tasks.Model;

public interface ITaskInputOutputInfo
{
    ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
public interface ITaskInputInfo : ITaskInputOutputInfo
{
    TaskInputType Type { get; }
}
public interface ITaskOutputInfo : ITaskInputOutputInfo
{
    TaskOutputType Type { get; }
}