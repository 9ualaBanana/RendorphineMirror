namespace Node.Tasks.Handlers;

public interface ITaskHandler { }
public interface ITaskInputHandler : ITaskHandler
{
    TaskInputType Type { get; }

    ValueTask Download(ReceivedTask task, CancellationToken cancellationToken = default);


    /// <summary> Initialize placed task (e.g. start torrent seeding for uploading source files) </summary>
    ValueTask InitializePlacedTaskAsync(DbTaskFullState task) => ValueTask.CompletedTask;
}
public interface ITaskOutputHandler : ITaskHandler
{
    TaskOutputType Type { get; }

    ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken = default);

    /// <summary> Check tasks for completion and returns true if task needs to be set to Finished. <br/> By default returns true if the task state is <see cref="TaskState.Output"/> </summary>
    ValueTask<bool> CheckCompletion(DbTaskFullState task) => ValueTask.FromResult(task.State == TaskState.Output);


    ValueTask OnPlacedTaskCompleted(DbTaskFullState task) => ValueTask.CompletedTask;
}