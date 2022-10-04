namespace Node.Tasks.Exec;

public static class TaskHandler
{
    public static IEnumerable<ITaskHandler> HandlerList => Handlers;
    static readonly List<ITaskHandler> Handlers = new();
    static readonly Dictionary<TaskInputType, ITaskInputHandler> InputHandlers = new();
    static readonly Dictionary<TaskOutputType, ITaskOutputHandler> OutputHandlers = new();


    public static async Task InitializePlacedTasksAsync()
    {
        TaskRegistration.TaskRegistered += task => InitializePlacedTaskAsync(task).Consume();
        await Task.WhenAll(NodeSettings.PlacedTasks.ToArray().Select(InitializePlacedTaskAsync));
    }
    public static async Task InitializePlacedTaskAsync(DbTaskFullState task)
    {
        if (await task.RemoveIfFinished())
            return;

        await task.GetInputHandler().InitializePlacedTaskAsync(task);
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

                foreach (var task in NodeSettings.QueuedTasks.ToArray())
                    HandleAsync(task).Consume();
            }
        })
        { IsBackground = true }.Start();
    }
    /// <summary> Polls all non-finished placed tasks, set their state to Finished if finished </summary>
    public static void StartUpdatingTaskState()
    {
        new Thread(async () =>
        {
            while (true)
            {
                foreach (var task in NodeSettings.PlacedTasks.Bindable.ToArray())
                {
                    try { await check(task); }
                    catch (Exception ex) { task.LogErr(ex); }
                }

                NodeSettings.PlacedTasks.Save();
                await Task.Delay(30_000);
            }
        })
        { IsBackground = true }.Start();


        async ValueTask check(DbTaskFullState task)
        {
            if (task.State == TaskState.Finished) return;

            try
            {
                var handler = task.GetOutputHandler();
                var completed = await handler.CheckCompletion(task);
                if (!completed) return;

                task.LogInfo("Completed");

                await handler.OnPlacedTaskCompleted(task);
                (await task.ChangeStateAsync(TaskState.Finished)).ThrowIfError();
                NodeSettings.PlacedTasks.Bindable.Remove(task);
            }
            catch (Exception ex) when (ex.Message.Contains("no task with such ", StringComparison.OrdinalIgnoreCase))
            {
                task.LogErr("Placed task does not exists on the server, removing");
                NodeSettings.PlacedTasks.Bindable.Remove(task);
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid old task state", StringComparison.OrdinalIgnoreCase))
            {
                task.LogErr(ex.Message + ", removing");
                NodeSettings.PlacedTasks.Bindable.Remove(task);
            }
        }
    }

    public static void StartWatchingTasks()
    {
        foreach (var task in NodeSettings.WatchingTasks.Bindable)
            task.StartWatcher();
    }



    public static async ValueTask<OperationResult<string>> RegisterOrExecute(TaskCreationInfo info)
    {
        OperationResult<string> taskid;
        if (info.ExecuteLocally)
        {
            taskid = ReceivedTask.GenerateLocalId();

            info.TaskObject ??= (await TaskModels.DeserializeInput(info.Input).GetFileInfo());
            var tk = new ReceivedTask(taskid.Value, new TaskInfo(info.TaskObject, info.Input, info.Output, info.Data, TaskPolicy.SameNode, Settings.Guid), true);
            NodeSettings.QueuedTasks.Bindable.Add(tk);
        }
        else taskid = await TaskRegistration.RegisterAsync(info).ConfigureAwait(false);

        return taskid;
    }
    static async Task HandleAsync(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        if (NodeGlobalState.Instance.ExecutingTasks.Contains(task))
            return;

        if (await task.RemoveIfFinished())
            return;

        const int maxattempts = 3;
        lock (NodeGlobalState.Instance.ExecutingTasks)
        {
            if (NodeGlobalState.Instance.ExecutingTasks.Contains(task))
                return;

            NodeGlobalState.Instance.ExecutingTasks.Add(task);
        }

        using var _ = new FuncDispose(() => NodeGlobalState.Instance.ExecutingTasks.Remove(task));
        task.LogInfo($"Started");

        for (int attempt = 0; attempt < maxattempts; attempt++)
        {
            try
            {
                var starttime = DateTimeOffset.Now;
                await TaskList.GetAction(task.Info).Execute(task).ConfigureAwait(false);

                var endtime = DateTimeOffset.Now;
                task.LogInfo($"Task completed in {(endtime - starttime)} and {attempt}/{maxattempts} attempts");
                NodeSettings.CompletedTasks.Add(task.Id, new CompletedTask(starttime, endtime, task) { Attempt = attempt });

                task.LogInfo($"Completed, removing");
                NodeSettings.QueuedTasks.Bindable.Remove(task);
                return;
            }
            catch (NodeTaskFailedException ex)
            {
                await setState(NodeSettings.FailedTasks, TaskState.Failed, attempt + 1, ex.Message);
                return;
            }
            catch (NodeTaskCanceledException ex)
            {
                await setState(NodeSettings.CanceledTasks, TaskState.Canceled, attempt + 1, ex.Message);
                return;
            }
            catch (Exception ex)
            {
                task.LogErr(ex);
                task.LogInfo($"Failed to execute task, retrying... ({attempt + 1}/{maxattempts})");
            }
        }

        await setState(NodeSettings.FailedTasks, TaskState.Failed, maxattempts, "Run out of attempts");


        async ValueTask setState(Settings.DatabaseValueList<ReceivedTask> newlist, TaskState state, int attempt, string message)
        {
            task.LogInfo($"Task requested to be {state} on attempt ({attempt + 1}/{maxattempts}): {message}");
            newlist.Bindable.Add(task);
            newlist.Save();
            NodeSettings.QueuedTasks.Bindable.Remove(task);

            var set = await task.ChangeStateAsync(state);
            if (set) task.LogInfo("Updated server task state");
            else task.LogWarn("Could not update task state on the server though");
        }
    }


    static async ValueTask<bool> RemoveIfFinished(this ReceivedTask task)
    {
        var finished = await test();
        if (finished)
        {
            task.LogInfo($"Removing");

            NodeSettings.QueuedTasks.Bindable.Remove(task);
            if (task is DbTaskFullState dbtask)
                NodeSettings.PlacedTasks.Bindable.Remove(dbtask);
        }

        return finished;


        async ValueTask<bool> test()
        {
            if (task.ExecuteLocally) return task.State.IsFinished();

            TaskState state;

            if (task.ExecuteLocally)
            {
                state = task.State;
                task.LogInfo($"Local/{task.State}");
            }
            else
            {
                var stater = await task.GetTaskStateAsync();
                if (!stater)
                {
                    stater.LogIfError();
                    if (stater.Message!.Contains("There is no task with such ID.", StringComparison.Ordinal))
                        state = TaskState.Failed;
                    else
                    {
                        stater.ThrowIfError();
                        return false;
                    }
                }
                else
                {
                    state = stater.Value.State;
                    task.LogInfo($"{stater.Value.State}/{task.State}");
                }
            }


            if (state.IsFinished())
            {
                if (task.State == TaskState.Output && state is not (TaskState.Canceled or TaskState.Failed))
                {
                    task.LogInfo($"Server task state was set to finished, but the result hasn't been uploaded yet");
                    return false;
                }

                return true;
            }

            return false;
        }
    }

    public static void AddHandler(ITaskHandler handler)
    {
        Handlers.Add(handler);

        if (handler is ITaskInputHandler inputh)
            InputHandlers[inputh.Type] = inputh;
        if (handler is ITaskOutputHandler outputh)
            OutputHandlers[outputh.Type] = outputh;
    }
    public static void AddHandlers(params ITaskHandler[] handlers)
    {
        foreach (var handler in handlers)
            AddHandler(handler);
    }


    public static ITaskInputHandler GetInputHandler(this ReceivedTask task) => (ITaskInputHandler) InputHandlers[task.Input.Type];
    public static ITaskOutputHandler GetOutputHandler(this ReceivedTask task) => (ITaskOutputHandler) OutputHandlers[task.Output.Type];
}
