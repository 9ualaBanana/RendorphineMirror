namespace Node.Tasks.Models;

public enum TaskInputOutputType
{
    User,
    MPlus,
}
public interface ITaskInputOutputInfo
{
    TaskInputOutputType Type { get; }
}
public interface ITaskInputInfo : ITaskInputOutputInfo
{
    ValueTask Upload();
    ValueTask<string> Download(ReceivedTask task, HttpClient httpClient, CancellationToken cancellationToken);
}
public interface ITaskOutputInfo : ITaskInputOutputInfo
{
    ValueTask Upload(ReceivedTask task, string file);
}
