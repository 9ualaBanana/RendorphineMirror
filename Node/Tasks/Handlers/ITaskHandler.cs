namespace Node.Tasks.Handlers;

public interface ITaskHandler { }
public interface ITaskInputHandler : ITaskHandler
{
    ImmutableArray<string>? AllowedTaskTypes => null;
    ImmutableArray<string>? DisallowedTaskTypes => null;

    TaskInputType Type { get; }

    ValueTask Download(ReceivedTask task, CancellationToken cancellationToken = default);


    /// <summary> Initialize placed task (e.g. start torrent seeding for uploading source files) </summary>
    ValueTask InitializePlacedTaskAsync(DbTaskFullState task) => ValueTask.CompletedTask;
}
public interface ITaskOutputHandler : ITaskHandler
{
    ImmutableArray<string>? AllowedTaskTypes => null;
    ImmutableArray<string>? DisallowedTaskTypes => null;

    TaskOutputType Type { get; }

    ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken = default);

    /// <summary> Check tasks for completion and returns true if task needs to be set to Finished. <br/> By default returns true if the task state is <see cref="TaskState.Output"/> </summary>
    async ValueTask<bool> CheckCompletion(DbTaskFullState task) => (await task.GetTaskStateAsync()).ThrowIfError().State == TaskState.Output;


    ValueTask OnPlacedTaskCompleted(DbTaskFullState task) => ValueTask.CompletedTask;
}