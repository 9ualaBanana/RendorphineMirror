namespace Node.Tasks.Watching;

public class MPlusAllFilesWatchingTaskHandler : MPlusWatchingTaskHandler<MPlusAllFilesWatchingTaskInputInfo>
{
    public override WatchingTaskInputType Type => WatchingTaskInputType.MPlusAllFiles;

    public MPlusAllFilesWatchingTaskHandler(WatchingTask task) : base(task) { }

    protected override ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync() =>
        Api.ApiGet<ImmutableArray<MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getnewitems", "items", "Getting new items", ("sessionid", Settings.SessionId!), ("sinceiid", Input.SinceIid ?? string.Empty));

    protected override ValueTask TickItem(MPlusNewItem item)
    {
        var fileName = item.Files.Jpeg.FileName;
        if (Input.SkipWatermarked && isWatermarked())
        {
            Task.LogInfo($"File {item.Iid} {Path.ChangeExtension(fileName, null)} is already watermarked, skipping");
            return ValueTask.CompletedTask;
        }

        return base.TickItem(item);


        bool isWatermarked()
        {
            if (item.QSPreview is null) return false;
            if (item.Files.Mov is not null && item.QSPreview.Mp4 is null) return false;

            return true;
        }
    }
    protected override async ValueTask Tick()
    {
        async Task<DbTaskFullState[]> fetch(TaskState state) => (await Apis.GetMyTasksAsync(state)).ThrowIfError().Where(x => x.Action == Task.TaskAction).ToArray();

        var fqueued = await fetch(TaskState.Queued);
        var finput = await fetch(TaskState.Input);
        var factive = await fetch(TaskState.Active);

        if (fqueued.Length != 0 || finput.Length != 0 || factive.Length != 0)
        {
            Task.LogInfo($"Found {fqueued.Length}Q {finput.Length}I {factive.Length}A pending ptasks, skipping fetching from {GetType().Name}");
            return;
        }


        Task.LogInfo($"No pending ptasks found, fetching new items since {Input.SinceIid ?? "<start>"}");
        await base.Tick();
    }
}