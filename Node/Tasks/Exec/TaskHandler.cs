using System.Runtime.Serialization;

namespace Node.Tasks.Exec;

public static class TaskHandler
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static IEnumerable<ITaskInputHandler> InputHandlerList => InputHandlers.Values;
    public static IEnumerable<ITaskOutputHandler> OutputHandlerList => OutputHandlers.Values;

    static readonly Dictionary<TaskInputType, ITaskInputHandler> InputHandlers = new();
    static readonly Dictionary<TaskOutputType, ITaskOutputHandler> OutputHandlers = new();
    static readonly Dictionary<WatchingTaskInputType, Func<WatchingTask, IWatchingTaskInputHandler>> WatchingHandlers = new();


    public static async Task InitializePlacedTasksAsync()
    {
        TaskRegistration.TaskRegistered += task => UploadInputFiles(task).Consume();
        await Task.WhenAll(NodeSettings.PlacedTasks.Values.ToArray().Select(UploadInputFiles));


        static async Task UploadInputFiles(DbTaskFullState task)
        {
            while (true)
            {
                var timeout = DateTime.Now.AddMinutes(5);
                while (timeout > DateTime.Now)
                {
                    var state = await Apis.Default.WithNoErrorLog().GetTaskStateAsync(task);
                    NodeSettings.PlacedTasks.Save(task);
                    if (task.State.IsFinished()) return;

                    if (state && state.Value is not null)
                    {
                        task.State = state.Value.State;
                        break;
                    }

                    await Task.Delay(2_000);
                }

                if (timeout < DateTime.Now) task.ThrowFailed("Could not get shard info for 5 min");
                if (task.State > TaskState.Input) return;

                try
                {
                    await task.GetInputHandler().UploadInputFiles(task);
                    return;
                }
                catch (Exception ex)
                {
                    task.LogErr(ex);
                    await Task.Delay(10_000);
                }
            }
        }
    }

    /// <summary> Subscribes to <see cref="NodeSettings.QueuedTasks"/> and starts all the tasks from it </summary>
    public static void StartListening()
    {
        new Thread(async () =>
        {
            while (true)
            {
                await Task.Delay(2_000);
                if (NodeSettings.QueuedTasks.Count == 0) continue;

                foreach (var task in NodeSettings.QueuedTasks.Values.ToArray())
                    HandleAsync(task).Consume();
            }
        })
        { IsBackground = true }.Start();
    }
    /// <summary> Polls all non-finished placed tasks, sets their state to Finished, Canceled, Failed if needed </summary>
    public static void StartUpdatingPlacedTasks()
    {
        var scguid = null as string;

        new Thread(async () =>
        {
            while (true)
            {
                try { await checkAll(); }
                catch (Exception ex) { Logger.Error(ex); }

                await Task.Delay(30_000);
            }
        })
        { IsBackground = true }.Start();


        async ValueTask checkAll()
        {
            if (NodeSettings.PlacedTasks.Count == 0) return;

            var copy = NodeSettings.PlacedTasks.Values.ToArray();
            var emptysharded = copy.Where(x => x.HostShard is null).ToArray();
            await Apis.Default.UpdateTaskShardsAsync(emptysharded).ThrowIfError();

            foreach (var es in emptysharded)
                NodeSettings.PlacedTasks.Save(es);

            copy = copy.Where(x => x.HostShard is not null).ToArray();

            var checkedtasks = new List<string>();
            foreach (var group in copy.GroupBy(x => x.HostShard))
            {
                var act = await Apis.Default.GetTasksOnShardAsync(group.Key!).ThrowIfError();
                if (scguid is null) scguid = act.ScGuid;
                else if (scguid != act.ScGuid)
                {
                    scguid = act.ScGuid;
                    Logger.Info($"!! Task configuration changed ({act.ScGuid}), resetting all task shards...");
                    foreach (var task in NodeSettings.PlacedTasks)
                        task.Value.HostShard = null;

                    return;
                }

                await process(TaskState.Input, act.Input);
                await process(TaskState.Active, act.Active);
                await process(TaskState.Output, act.Output);
                await process(TaskState.Validation, act.Validation);

                async ValueTask process(TaskState state, ImmutableArray<TMTaskStateInfo> tasks)
                {
                    var changed = tasks.Where(x => NodeSettings.PlacedTasks.TryGetValue(x.Id, out var task) && task.State != state).Select(x => x.Id).ToArray();
                    if (changed.Length != 0)
                        Logger.Info($"Tasks changed to {state}: {string.Join(", ", changed)}");

                    foreach (var task in tasks)
                        await processTask(task.Id, task, state);
                    checkedtasks.AddRange(tasks.Select(x => x.Id));
                }
            }

            var finished = await Apis.Default.GetFinishedTasksStatesAsync(copy.Select(x => x.Id).Except(checkedtasks)).ThrowIfError();
            foreach (var (id, state) in finished)
                await processTask(id, state, null);


            async ValueTask processTask(string taskid, ITaskStateInfo state, TaskState? newstate)
            {
                if (!NodeSettings.PlacedTasks.TryGetValue(taskid, out var task)) return;

                task.State = newstate ?? task.State;
                task.Populate(state);
                if (task.State == TaskState.Validation)
                    task.Populate(await task.GetTaskStateAsyncOrThrow().ThrowIfError());

                try { await check(task, null); }
                catch (NodeTaskFailedException ex)
                {
                    await task.ChangeStateAsync(TaskState.Canceled).ThrowIfError();
                    remove(task, ex.Message);
                }
                catch (Exception ex) { task.LogErr(ex); }
            }
        }
        async ValueTask check(DbTaskFullState task, string? errmsg)
        {
            if (task.State.IsFinished())
            {
                remove(task);
                return;
            }

            if (task.State < TaskState.Output) return;
            if (task.State is TaskState.Canceled or TaskState.Failed)
            {
                remove(task, errmsg);
                return;
            }

            var handler = task.GetOutputHandler();
            var completed = await handler.CheckCompletion(task);
            if (!completed) return;

            await handler.OnPlacedTaskCompleted(task);
            await task.ChangeStateAsync(TaskState.Finished).ThrowIfError();
            remove(task);
        }
        bool remove(DbTaskFullState task, string? errmsg = null)
        {
            task.LogInfo($"{task.State}, removing" + (errmsg is null ? null : $" ({errmsg})"));

            NodeSettings.PlacedTasks.Remove(task);
            foreach (var wtask in NodeSettings.WatchingTasks.Values.ToArray())
                if (wtask.PlacedNonCompletedTasks.Remove(task.Id))
                    NodeSettings.WatchingTasks.Save(wtask);

            return true;
        }
    }

    public static void StartWatchingTasks()
    {
        foreach (var task in NodeSettings.WatchingTasks.Values)
            task.StartWatcher();
    }


    static async Task HandleAsync(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        if (NodeGlobalState.Instance.ExecutingTasks.Contains(task))
            return;

        if (await isFinishedOnServer())
        {
            task.LogInfo($"{task.State}, removing");
            NodeSettings.QueuedTasks.Remove(task);

            return;
        }

        const int maxattempts = 3;
        lock (NodeGlobalState.Instance.ExecutingTasks)
        {
            if (NodeGlobalState.Instance.ExecutingTasks.Contains(task))
                return;

            NodeGlobalState.Instance.ExecutingTasks.Add(task);
        }

        using var _ = new FuncDispose(() => NodeGlobalState.Instance.ExecutingTasks.Remove(task));
        task.LogInfo($"Execution started");

        int attempt;
        for (attempt = 0; attempt < maxattempts; attempt++)
        {
            try
            {
                var starttime = DateTimeOffset.Now;
                await TaskList.GetAction(task.Info).Execute(task).ConfigureAwait(false);

                var endtime = DateTimeOffset.Now;
                task.LogInfo($"Task completed in {(endtime - starttime)} and {attempt}/{maxattempts} attempts");

                NodeSettings.CompletedTasks.Remove(task.Id);
                NodeSettings.CompletedTasks.Add(new CompletedTask(starttime, endtime, task) { Attempt = attempt });

                task.LogInfo($"Completed, removing");

                task.LogInfo($"Deleting {task.FSInputDirectory()}");
                Directory.Delete(task.FSInputDirectory(), true);

                NodeSettings.QueuedTasks.Remove(task);
                return;
            }
            catch (NodeTaskFailedException ex)
            {
                await fail(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                task.LogErr(ex);
                task.LogInfo($"Failed to execute task, retrying... ({attempt + 1}/{maxattempts})");
            }
        }

        await fail("Ran out of attempts");


        async ValueTask fail(string message)
        {
            task.LogInfo($"Task was failed ({attempt + 1}/{maxattempts}): {message}");
            await task.FailTaskAsync(message).ThrowIfError();

            task.LogInfo($"Deleting {task.FSInputDirectory()} {task.FSOutputDirectory()}");
            Directory.Delete(task.FSInputDirectory(), true);
            Directory.Delete(task.FSOutputDirectory(), true);

            NodeSettings.QueuedTasks.Remove(task);
        }
        async ValueTask<bool> isFinishedOnServer()
        {
            var state = await task.GetTaskStateAsync().ThrowIfError();
            // Since we are the executor, if state is null, then task state can only be Canceled. Or Finished, but that would be a bug from the task creator node.

            if (state is not null)
            {
                task.LogInfo($"R {state.State}/L {task.State}");
                if (task.State == TaskState.Queued)
                    task.State = state.State;
            }

            if (state?.State == TaskState.Finished && task.State == TaskState.Validation)
            {
                task.LogErr($"Server task state was set to finished, but the result hasn't been uploaded yet (!! bug from the task creator node !!)");
                return false;
            }

            var finished = state is null || state.State.IsFinished();
            if (finished && state is not null) task.State = state.State;

            return finished;
        }
    }

    public static void AutoInitializeHandlers()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(ass => ass.GetTypes())
            .Where(x => x.IsAssignableTo(typeof(ITaskHandler)))
            .Where(x => x.IsClass && !x.IsAbstract);

        foreach (var type in types)
            AddHandler(type);
    }
    public static void AddHandler(Type type)
    {
        if (type.IsAssignableTo(typeof(ITaskInputHandler)))
        {
            var handler = (ITaskInputHandler) Activator.CreateInstance(type)!;
            InputHandlers[handler.Type] = handler;
        }
        if (type.IsAssignableTo(typeof(ITaskOutputHandler)))
        {
            var handler = (ITaskOutputHandler) Activator.CreateInstance(type)!;
            OutputHandlers[handler.Type] = handler;
        }
        if (type.IsAssignableTo(typeof(IWatchingTaskInputHandler)))
        {
            // FormatterServices.GetSafeUninitializedObject is being used to create valid object for getting only the .Type property
            // since all IWatchingTaskInputHandler object implement Type property using `=>` and not `{ get; } =`

            WatchingHandlers[((IWatchingTaskInputHandler) FormatterServices.GetSafeUninitializedObject(type)).Type] =
                task => (IWatchingTaskInputHandler) Activator.CreateInstance(type, new object?[] { task })!;
        }
    }


    public static ITaskInputHandler GetInputHandler(this TaskBase task) => InputHandlers[task.Input.Type];
    public static ITaskOutputHandler GetOutputHandler(this TaskBase task) => OutputHandlers[task.Output.Type];
    public static IWatchingTaskInputHandler CreateWatchingHandler(this WatchingTask task) => WatchingHandlers[task.Source.Type](task);
}
