namespace Node.Tasks.Watching;

public abstract class MPlusWatchingTaskHandler<TData> : WatchingTaskHandler<TData> where TData : IMPlusWatchingTaskInputInfo
{
    protected MPlusWatchingTaskHandler(WatchingTask task) : base(task) { }

    public override void StartListening() => StartThreadRepeated(60_000, Tick);
    protected virtual async ValueTask Tick()
    {
        var items = (await FetchItemsAsync()).ThrowIfError();
        if (items.Length == 0) return;

        foreach (var item in items.OrderBy<MPlusNewItem, long>(x => x.Registered))
        {
            await TickItem(item);
            Input.SinceIid = item.Iid;
        }

        if (Task.PlacedNonCompletedTasks.Count == 0) await Tick();
    }
    protected abstract ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync();
    protected virtual async ValueTask TickItem(MPlusNewItem item)
    {
        var fileName = item.Files.Jpeg.FileName;
        Task.LogInfo($"Adding new file {item.Iid} {Path.ChangeExtension(fileName, null)}");

        var output =
            (Task.Output as IMPlusWatchingTaskOutputInfo)?.CreateOutput(Task, item, fileName)
            ?? Task.Output.CreateOutput(Task, fileName);

        var newinput = new MPlusTaskInputInfo(item.Iid, item.UserId);
        var newtask = await Task.RegisterTask(newinput, output);
        Task.PlacedNonCompletedTasks.Add(newtask.Id);

        SaveTask();
    }
}