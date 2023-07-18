namespace Node.Tasks.Watching;

public abstract class MPlusWatchingTaskHandler<TData> : WatchingTaskHandler<TData> where TData : IMPlusWatchingTaskInputInfo
{
    protected MPlusWatchingTaskHandler(WatchingTask task) : base(task) { }

    public override void StartListening() => StartThreadRepeated(60_000, Tick);
    protected virtual async ValueTask Tick()
    {
        var items = (await FetchItemsAsync()).ThrowIfError();
        if (items.Length == 0) return;

        var taskobjs = await items
            .GroupBy(i => i.UserId)
            .Select(async g => await MPlusTaskInputInfo.GetFilesInfoDict(Settings.SessionId, g.Key, g.Select(i => i.Iid)))
            .MergeDictResults()
            .ThrowIfError();

        foreach (var item in items)
        {
            if (!taskobjs.ContainsKey(item.Iid))
            {
                Task.LogWarn($"Task objects does not contain item {item.Iid} {item}");
                continue;
            }

            await TickItem(item, taskobjs[item.Iid]);

            if (Input is MPlusWatchingTaskInputInfo mpinfo)
                mpinfo.SinceIid = item.Iid;
        }

        if (Task.PlacedNonCompletedTasks.Count == 0) await Tick();
    }
    protected abstract ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync();
    protected virtual async ValueTask TickItem(MPlusNewItem item, TaskObject taskobj)
    {
        var fileName = item.Files.Jpeg.FileName;
        Task.LogInfo($"Adding new file [userid: {item.UserId}; iid: {item.Iid}] {Path.ChangeExtension(fileName, null)}");

        var output =
            (Task.Output as IMPlusWatchingTaskOutputInfo)?.CreateOutput(Task, item, fileName)
            ?? Task.Output.CreateOutput(Task, fileName);

        var newinput = new MPlusTaskInputInfo(item.Iid, item.UserId);
        var newtask = await Task.RegisterTask(newinput, output, taskobj);
        Task.PlacedNonCompletedTasks.Add(newtask.Id);

        SaveTask();
    }
}