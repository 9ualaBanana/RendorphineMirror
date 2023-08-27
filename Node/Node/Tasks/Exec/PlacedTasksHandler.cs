namespace Node.Tasks.Exec;

public class PlacedTasksHandler
{
    public required IPlacedTasksStorage PlacedTasks { get; init; }
    public required IWatchingTasksStorage WatchingTasks { get; init; }
    public required Apis Api { get; init; }
    public required IIndex<TaskInputType, ITaskInputUploader> InputUploaders { get; init; }
    public required IIndex<TaskOutputType, ITaskCompletionChecker> CompletionCheckers { get; init; }
    public required IIndex<TaskOutputType, ITaskCompletionHandler> CompletionHandlers { get; init; }
    public required ILogger<PlacedTasksHandler> Logger { get; init; }

    public async Task InitializePlacedTasksAsync() =>
        await Task.WhenAll(PlacedTasks.PlacedTasks.Values.ToArray().Select(UploadInputFiles));

    public async Task UploadInputFiles(DbTaskFullState task)
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
                if (InputUploaders.TryGetValue(task.Input.Type, out var handler))
                    await handler.Upload(task.Input);

                return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "");
                await Task.Delay(10_000);
            }
        }
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
                    task.Populate(await Api.GetTaskStateAsyncOrThrow(task).ThrowIfError());

                try { await check(task, (state as TMOldTaskStateInfo)?.ErrMsg); }
                catch (TaskFailedException ex)
                {
                    await Api.ChangeStateAsync(task, TaskState.Canceled).ThrowIfError();
                    remove(task, ex.Message);
                }
                catch (Exception ex) { Logger.LogError(ex, ""); }
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

            var completed =
                CompletionCheckers.TryGetValue(task.Output.Type, out var checker)
                ? checker.CheckCompletion(task.Output, task.State)
                : task.State == TaskState.Validation;
            if (!completed) return;

            if (CompletionHandlers.TryGetValue(task.Output.Type, out var handler))
                await handler.OnPlacedTaskCompleted(task.Output);

            await Api.ChangeStateAsync(task, TaskState.Finished).ThrowIfError();
            remove(task, errmsg);
        }
        bool remove(DbTaskFullState task, string? errmsg)
        {
            Logger.LogInformation($"{task.State}, removing" + (errmsg is null ? null : $" ({errmsg})"));
            PlacedTasks.PlacedTasks.Remove(task);

            foreach (var wtask in WatchingTasks.WatchingTasks.Values.ToArray())
            {
                if (!wtask.PlacedNonCompletedTasks.Contains(task.Id)) continue;

                wtask.Complete(task);
                wtask.PlacedNonCompletedTasks.Remove(task.Id);
                WatchingTasks.WatchingTasks.Save(wtask);
            }

            if (errmsg?.Contains("There is no such user.", StringComparison.Ordinal) == true)
            {
                var tuid = (task.Output as MPlusTaskOutputInfo)?.TUid ?? (task.Output as QSPreviewOutputInfo)?.TUid;
                if (tuid is not null)
                {
                    Logger.LogWarning($"Found nonexistent user {tuid}, hiding");

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
}
