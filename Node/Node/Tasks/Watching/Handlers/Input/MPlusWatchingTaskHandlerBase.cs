namespace Node.Tasks.Watching.Handlers.Input;

public abstract class MPlusWatchingTaskHandlerBase<TInput> : WatchingTaskInputHandler<TInput>
    where TInput : IMPlusWatchingTaskInputInfo
{
    public required Api Api { get; init; }

    public override void StartListening() => StartThreadRepeated(60_000, Tick);

    protected virtual async Task Tick()
    {
        var items = (await FetchItemsAsync()).ThrowIfError();
        if (items.Length == 0) return;

        var taskobjs = await items
            .GroupBy(i => i.UserId)
            .Select(async g => await MPlusTaskInputInfo.GetFilesInfoDict(Api, Settings.SessionId, g.Key, g.Select(i => i.Iid)))
            .AggregateMany()
            .ThrowIfError();

        foreach (var item in items)
        {
            if (!taskobjs.ContainsKey(item.Iid))
            {
                Logger.LogWarning($"Task objects does not contain item {item.Iid} {item}");
                continue;
            }

            await TickItem(item, taskobjs[item.Iid]);

            if (Input is MPlusWatchingTaskInputInfo mpinfo)
                mpinfo.SinceIid = item.Iid;
        }

        if (Task.PlacedNonCompletedTasks.Count == 0) await Tick();
    }
    protected abstract Task<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync();

    protected virtual async Task TickItem(MPlusNewItem item, TaskObject taskobj)
    {
        var fileName = item.Files.Jpeg.FileName;
        Logger.LogInformation($"Adding new file [userid: {item.UserId}; iid: {item.Iid}] {Path.ChangeExtension(fileName, null)}");

        var output =
            (Task.Output as IMPlusWatchingTaskOutputInfo)?.CreateOutput(Task, item, fileName)
            ?? Task.Output.CreateOutput(Task, fileName);

        var newinput = new MPlusTaskInputInfo(item.Iid, item.UserId);
        var newtask = await Register(newinput, output, taskobj);
        Task.PlacedNonCompletedTasks.Add(newtask.Id);

        SaveTask();
    }

    protected virtual async Task<DbTaskFullState> Register(MPlusTaskInputInfo input, ITaskOutputInfo output, TaskObject tobj) =>
        await TaskRegistration.RegisterAsync(Task, input, output, tobj);
}