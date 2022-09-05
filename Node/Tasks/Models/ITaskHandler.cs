namespace Node.Tasks.Models;

public interface ITaskHandler
{
    TaskInputOutputType Type { get; }
}
public interface ITaskInputHandler : ITaskHandler
{
    ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken);
}
public interface ITaskOutputHandler : ITaskHandler
{
    ValueTask UploadResult(ReceivedTask task, string file, string? postfix, CancellationToken cancellationToken);
}

public interface ITaskCompletionCheckHandler : ITaskHandler
{
    /// <summary> Check tasks for completion and returns true if task needs to be set to Finished </summary>
    ValueTask<bool> CheckCompletion(DbTaskFullState task);
}