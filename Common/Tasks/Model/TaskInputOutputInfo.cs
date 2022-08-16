namespace Common.Tasks.Model;

public interface ITaskInputOutputInfo
{
    TaskInputOutputType Type { get; }
}
public interface ITaskInputInfo : ITaskInputOutputInfo { }
public interface ITaskOutputInfo : ITaskInputOutputInfo { }
