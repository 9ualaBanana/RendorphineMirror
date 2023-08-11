using Node.Tasks.Watching.Handlers.Input;

namespace Node.Tasks.Exec;

public class TaskHandler
{
    public required ILifetimeScope ComponentContext { get; init; }
    public required IPlacedTasksStorage PlacedTasks { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required IWatchingTasksStorage WatchingTasks { get; init; }
    public required PluginManager PluginManager { get; init; }
    public required NodeCommon.Apis Api { get; init; }
    public required NodeGlobalState NodeGlobalState { get; init; }
    public required ILogger<TaskHandler> Logger { get; init; }


    public async Task InitializePlacedTasksAsync()
    {
        NodeCommon.Tasks.TaskRegistration.TaskRegistered += task => UploadInputFiles(task).Consume();
        await Task.WhenAll(PlacedTasks.PlacedTasks.Values.ToArray().Select(UploadInputFiles));


        async Task UploadInputFiles(DbTaskFullState task)
        {
            while (true)
            {
                var timeout = DateTime.Now.AddMinutes(5);
                while (timeout > DateTime.Now)
                {
                    var state = await Api.WithNoErrorLog().GetTaskStateAsync(task);
                    PlacedTasks.PlacedTasks.Save(task);
                    if (task.State.IsFinished()) return;

                    if (state && state.Value is not null)
                    {
                        task.State = state.Value.State;
                        break;
                    }

                    await Task.Delay(2_000);
                }

                if (timeout < DateTime.Now) throw new TaskFailedException("Could not get shard info for 5 min");
                if (task.State > TaskState.Input) return;

                try
                {
                    var handler = ComponentContext.ResolveKeyed<ITaskInputUploader>(task.Input.Type);
                    await handler.Upload(task.Input);
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

    /// <summary> Subscribes to <see cref="QueuedTasks.QueuedTasks"/> and starts all the tasks from it </summary>
    public void StartListening()
    {
        new Thread(async () =>
        {
            while (true)
            {
                await Task.Delay(2_000);
                if (QueuedTasks.QueuedTasks.Count == 0) continue;

                foreach (var task in QueuedTasks.QueuedTasks.Values.ToArray())
                    HandleAsync(task).Consume();
            }
        })
        { IsBackground = true }.Start();
    }
    /// <summary> Polls all non-finished placed tasks, sets their state to Finished, Canceled, Failed if needed </summary>
    public void StartUpdatingPlacedTasks()
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
            if (PlacedTasks.PlacedTasks.Count == 0) return;

            var copy = PlacedTasks.PlacedTasks.Values.ToArray();
            var emptysharded = copy.Where(x => x.HostShard is null).ToArray();
            await Api.UpdateTaskShardsAsync(emptysharded).ThrowIfError();

            foreach (var es in emptysharded)
                PlacedTasks.PlacedTasks.Save(es);

            copy = copy.Where(x => x.HostShard is not null).ToArray();

            var checkedtasks = new List<string>();
            foreach (var group in copy.GroupBy(x => x.HostShard))
            {
                var act = await Api.GetTasksOnShardAsync(group.Key!).ThrowIfError();
                if (scguid is null) scguid = act.ScGuid;
                else if (scguid != act.ScGuid)
                {
                    scguid = act.ScGuid;
                    Logger.Info($"!! Task configuration changed ({act.ScGuid}), resetting all task shards...");
                    foreach (var task in PlacedTasks.PlacedTasks)
                        task.Value.HostShard = null;

                    return;
                }

                await process(TaskState.Input, act.Input);
                await process(TaskState.Active, act.Active);
                await process(TaskState.Output, act.Output);
                await process(TaskState.Validation, act.Validation);

                async ValueTask process(TaskState state, ImmutableArray<TMTaskStateInfo> tasks)
                {
                    var changed = tasks.Where(x => PlacedTasks.PlacedTasks.TryGetValue(x.Id, out var task) && task.State != state).Select(x => x.Id).ToArray();
                    if (changed.Length != 0)
                        Logger.Info($"Tasks changed to {state}: {string.Join(", ", changed)}");

                    foreach (var task in tasks)
                        await processTask(task.Id, task, state);
                    checkedtasks.AddRange(tasks.Select(x => x.Id));
                }
            }

            var finished = await Api.GetFinishedTasksStatesAsync(copy.Select(x => x.Id).Except(checkedtasks)).ThrowIfError();
            foreach (var (id, state) in finished)
                await processTask(id, state, null);


            async ValueTask processTask(string taskid, ITaskStateInfo state, TaskState? newstate)
            {
                if (!PlacedTasks.PlacedTasks.TryGetValue(taskid, out var task)) return;

                task.State = newstate ?? task.State;
                task.Populate(state);
                if (task.State == TaskState.Validation)
                    task.Populate(await task.GetTaskStateAsyncOrThrow().ThrowIfError());

                try { await check(task, (state as TMOldTaskStateInfo)?.ErrMsg); }
                catch (TaskFailedException ex)
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
                remove(task, errmsg);
                return;
            }

            if (task.State < TaskState.Output) return;
            if (task.State is TaskState.Canceled or TaskState.Failed)
            {
                remove(task, errmsg);
                return;
            }

            var checker = ComponentContext.ResolveKeyed<ITaskCompletionChecker>(task.Input.Type);
            var completed = checker.CheckCompletion(task.Output, task.State);
            if (!completed) return;

            var handler = ComponentContext.ResolveKeyed<ITaskCompletionHandler>(task.Input.Type);
            await handler.OnPlacedTaskCompleted(task.Output);
            await task.ChangeStateAsync(TaskState.Finished).ThrowIfError();
            remove(task, errmsg);
        }
        bool remove(DbTaskFullState task, string? errmsg)
        {
            task.LogInfo($"{task.State}, removing" + (errmsg is null ? null : $" ({errmsg})"));
            PlacedTasks.PlacedTasks.Remove(task);

            foreach (var wtask in WatchingTasks.WatchingTasks.Values.ToArray())
            {
                wtask.PlacedNonCompletedTasks.Remove(task.Id);
                WatchingTasks.WatchingTasks.Save(wtask);
            }

            if (errmsg?.Contains("There is no such user.", StringComparison.Ordinal) == true)
            {
                var tuid = (task.Output as MPlusTaskOutputInfo)?.TUid ?? (task.Output as QSPreviewOutputInfo)?.TUid;
                if (tuid is not null)
                {
                    task.LogWarn($"Found nonexistent user {tuid}, hiding");

                    foreach (var wtask in WatchingTasks.WatchingTasks.Values.ToArray())
                    {
                        if (wtask.Source is MPlusAllFilesWatchingTaskInputInfo handler)
                            handler.NonexistentUsers.Add(tuid);

                        WatchingTasks.WatchingTasks.Save(wtask);
                    }
                }
            }

            return true;
        }
    }

    public void StartWatchingTasks()
    {
        foreach (var task in WatchingTasks.WatchingTasks.Values)
            StartWatchingTask(task);
    }
    public void StartWatchingTask(WatchingTask task)
    {
        Logger.LogInformation($"Watcher started; Data: {JsonConvert.SerializeObject(task, Init.DebugFeatures ? JsonSettings.Typed : new JsonSerializerSettings())}");

        var handler = CreateWatchingHandler(task);
        handler.StartListening();


        IWatchingTaskInputHandler CreateWatchingHandler(WatchingTask task)
        {
            using var scope = ComponentContext.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(task)
                    .SingleInstance();
            });

            return scope.ResolveKeyed<IWatchingTaskInputHandler>(Enum.Parse<TaskAction>(task.TaskAction));
        }
    }


    async Task HandleAsync(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        if (task is null)
        {
            // i dont even know
            NodeGlobalState.QueuedTasks.Remove(task!);
            return;
        }

        if (NodeGlobalState.ExecutingTasks.Contains(task))
            return;

        if (await isFinishedOnServer())
        {
            task.LogInfo($"{task.State}, removing");
            QueuedTasks.QueuedTasks.Remove(task);

            return;
        }

        const int maxattempts = 3;
        lock (NodeGlobalState.ExecutingTasks)
        {
            if (NodeGlobalState.ExecutingTasks.Contains(task))
                return;

            NodeGlobalState.ExecutingTasks.Add(task);
        }

        using var _ = new FuncDispose(() => NodeGlobalState.ExecutingTasks.Remove(task));
        task.LogInfo($"Execution started");

        var lastexception = null as Exception;
        int attempt;
        for (attempt = 0; attempt < maxattempts; attempt++)
        {
            try
            {
                var starttime = DateTimeOffset.Now;

                using var scope = ComponentContext.BeginLifetimeScope(builder =>
                {
                    builder.RegisterInstance(task)
                        .AsSelf()
                        .As<IRegisteredTask>()
                        .As<IRegisteredTaskApi>()
                        .As<IMPlusTask>()
                        .SingleInstance();

                    builder.RegisterType<TaskProgressSetter>()
                        .As<ITaskProgressSetter>()
                        .SingleInstance();

                    builder.RegisterDecorator<ITaskProgressSetter>((ctx, parameters, instance) => new ThrottledProgressSetter(TimeSpan.FromSeconds(5), instance));
                });

                var executor = scope.Resolve<TaskExecutor>();
                await executor.Execute(cancellationToken).ConfigureAwait(false);

                var endtime = DateTimeOffset.Now;
                task.LogInfo($"Task completed in {(endtime - starttime)} and {attempt}/{maxattempts} attempts");

                CompletedTasks.CompletedTasks.Remove(task.Id);
                CompletedTasks.CompletedTasks.Add(new CompletedTask(starttime, endtime, task) { Attempt = attempt });

                task.LogInfo($"Completed, removing");

                task.LogInfo($"Deleting {task.FSDataDirectory()}");
                Directory.Delete(task.FSDataDirectory(), true);

                QueuedTasks.QueuedTasks.Remove(task);
                return;
            }
            catch (TaskFailedException ex)
            {
                await fail(ex.Message, $"{ex.FullError}; at {ex.TargetSite}; {ex}");
                return;
            }
            catch (Exception ex)
            {
                task.LogErr(ex);
                task.LogInfo($"Failed to execute task, retrying... ({attempt + 1}/{maxattempts})");

                lastexception = ex;
            }
        }

        await fail($"Ran out of attempts: {lastexception?.Message}", $"at {lastexception?.TargetSite}; {lastexception}");


        async ValueTask fail(string errmsg, string fullerrmsg)
        {
            task.LogInfo($"Task was failed ({attempt + 1}/{maxattempts}): {fullerrmsg}");
            await task.FailTaskAsync(errmsg, fullerrmsg).ThrowIfError();

            /*
            task.LogInfo($"Deleting {task.FSInputDirectory()} {task.FSOutputDirectory()}");
            Directory.Delete(task.FSInputDirectory(), true);
            Directory.Delete(task.FSOutputDirectory(), true);
            */

            QueuedTasks.QueuedTasks.Remove(task);
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


    class TaskProgressSetter : ITaskProgressSetter
    {
        readonly NodeCommon.Apis Api;
        readonly IMPlusTask Task;

        public TaskProgressSetter(NodeCommon.Apis api, IMPlusTask task)
        {
            Api = api;
            Task = task;
        }

        public void Set(double progress)
        {
            if (Task is ReceivedTask rt) rt.Progress = progress;
            Api.SendTaskProgressAsync(Task).Consume();
        }
    }
}
