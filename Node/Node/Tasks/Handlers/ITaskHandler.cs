namespace Node.Tasks.Handlers;

public interface ITaskHandler { }
public interface ITaskInputHandler : ITaskHandler
{
    TaskInputType Type { get; }

    ValueTask<ReadOnlyTaskFileList> Download(ReceivedTask task, CancellationToken cancellationToken = default);


    /// <summary> Initialize placed task (e.g. start torrent seeding for uploading source files) </summary>
    ValueTask UploadInputFiles(DbTaskFullState task) => ValueTask.CompletedTask;
    ValueTask<OperationResult<TaskObject>> GetTaskObject(ITaskInputInfo input);
}
public interface ITaskOutputHandler : ITaskHandler
{
    TaskOutputType Type { get; }

    ValueTask UploadResult(ReceivedTask task, ReadOnlyTaskFileList files, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check tasks for completion and returns true if task needs to be set to Finished.
    /// By default returns true if the task state is <see cref="TaskState.Validation"/>.
    /// Assumes all the task properties are updated.
    /// </summary>
    ValueTask<bool> CheckCompletion(DbTaskFullState task) => ValueTask.FromResult(task.State == TaskState.Validation);


    ValueTask OnPlacedTaskCompleted(DbTaskFullState task) => ValueTask.CompletedTask;
}