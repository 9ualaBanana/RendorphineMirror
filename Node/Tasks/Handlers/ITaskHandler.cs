namespace Node.Tasks.Handlers;

public interface ITaskHandler { }
public interface ITaskInputHandler : ITaskHandler
{
    TaskInputType Type { get; }

    ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken = default);
}
public interface ITaskOutputHandler : ITaskHandler
{
    TaskOutputType Type { get; }

    ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken = default);
}

public interface IPlacedTaskCompletionCheckHandler : ITaskOutputHandler
{
    /// <summary> Check tasks for completion and returns true if task needs to be set to Finished </summary>
    ValueTask<bool> CheckCompletion(DbTaskFullState task);
}
public interface IPlacedTaskResultDownloadHandler : ITaskOutputHandler
{
    ValueTask DownloadResult(DbTaskFullState task);
}

public interface IPlacedTaskOnCompletedHandler : ITaskOutputHandler
{
    ValueTask OnPlacedTaskCompleted(DbTaskFullState task);
}
public interface IPlacedTaskInitializationHandler : ITaskInputHandler
{
    /// <summary> Initialize placed task (e.g. start torrent seeding for uploading source files) </summary>
    ValueTask InitializePlacedTaskAsync(DbTaskFullState task);
}

public interface ITaskTypeFilterHandler : ITaskHandler
{
    ImmutableArray<string>? AllowedTaskTypes { get; }
    ImmutableArray<string>? DisallowedTaskTypes { get; }
}