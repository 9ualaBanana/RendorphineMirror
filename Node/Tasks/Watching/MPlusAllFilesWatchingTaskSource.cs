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
        // fetch new items only if there is less than N ptasks pending
        const int taskFetchingThreshold = 50 - 1;

        if (Task.PlacedNonCompletedTasks.Count > taskFetchingThreshold)
            return;

        Task.LogInfo($"Found {Task.PlacedNonCompletedTasks.Count} unfinished ptasks found, fetching new items since {Input.SinceIid ?? "<start>"}");
        await base.Tick();
    }
}