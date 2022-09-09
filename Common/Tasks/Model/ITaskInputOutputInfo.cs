namespace Common.Tasks.Model;

public interface ITaskInputOutputInfo
{
    TaskInputOutputType Type { get; }

    ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
public interface ITaskInputInfo : ITaskInputOutputInfo { }
public interface ITaskOutputInfo : ITaskInputOutputInfo { }