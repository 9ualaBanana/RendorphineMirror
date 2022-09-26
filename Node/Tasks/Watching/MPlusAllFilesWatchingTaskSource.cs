namespace Node.Tasks.Watching;

public class MPlusAllFilesWatchingTaskSource : MPlusWatchingTaskSourceBase
{
    public override WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.MPlusAllFiles;

    public MPlusAllFilesWatchingTaskSource(string? sinceiid) : base(sinceiid) { }

    protected override ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync() =>
        Api.ApiGet<ImmutableArray<MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getnewitems", "items", "Getting new items", ("sessionid", Settings.SessionId!), ("sinceiid", SinceIid ?? string.Empty));

    protected override async Task Tick(WatchingTask task)
    {
        async Task<DbTaskFullState[]> fetch(TaskState state) => (await Apis.GetMyTasksAsync(state)).ThrowIfError().Where(x => x.Action == task.TaskAction).ToArray();

        var queued = await fetch(TaskState.Queued);
        var input = await fetch(TaskState.Input);
        var active = await fetch(TaskState.Active);

        if (queued.Length != 0 || input.Length != 0 || active.Length != 0)
        {
            task.LogInfo($"Found {queued.Length}Q {input.Length}I {active.Length}A pending ptasks, skipping fetching from {GetType().Name}");
            return;
        }


        task.LogInfo($"No pending ptasks found, fetching new items since {SinceIid ?? "<start>"}");
        await base.Tick(task);
    }
}
