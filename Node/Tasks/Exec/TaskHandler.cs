namespace Node.Tasks.Exec;

public static class TaskHandler
{
    public static IEnumerable<ITaskHandler> HandlerList => Handlers;
    static readonly List<ITaskHandler> Handlers = new();
    static readonly Dictionary<TaskInputType, ITaskInputHandler> InputHandlers = new();
    static readonly Dictionary<TaskOutputType, ITaskOutputHandler> OutputHandlers = new();


    public static async ValueTask InitializePlacedTasksAsync()
    {
        TaskRegistration.TaskRegistered += task => InitializePlacedTaskAsync(task).AsTask().Consume();

        foreach (var task in NodeSettings.PlacedTasks.ToArray())
            await InitializePlacedTaskAsync(task);
    }
    public static async ValueTask InitializePlacedTaskAsync(DbTaskFullState task)
    {
        if (await task.RemoveIfFinished())
            return;

        await (task.TryGetInputHandler<IPlacedTaskInitializationHandler>()?.InitializePlacedTaskAsync(task) ?? ValueTask.CompletedTask);
    }

    /// <summary> Subscribes to <see cref="NodeSettings.QueuedTasks"/> and starts all the tasks from it </summary>
    public static void StartListening()
    {
        new Thread(async () =>
        {
            int index = 0;

            while (true)
            {
                await Task.Delay(2_000);
                if (NodeSettings.QueuedTasks.Count == 0) continue;

                index = Math.Max(index, NodeSettings.QueuedTasks.Bindable.Count - 1);

                var task = NodeSettings.QueuedTasks.Bindable[index];
                try
                {
                    await HandleAsync(task);
                    index = 0;
                }
                catch (Exception ex)
                {
                    task.LogErr(ex);
                    task.LogInfo("Skipping a task");
                    index++;
                }
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
            var handler = task.TryGetOutputHandler<IPlacedTaskCompletionCheckHandler>();
            if (handler is null) return;

            try
            {
                var completed = await handler.CheckCompletion(task);
                if (!completed) return;

                task.LogInfo("Completed");

                if (task.TryGetOutputHandler<IPlacedTaskResultDownloadHandler>() is { } resultdownloader)
                {
                    task.LogInfo("Downloading result");
                    await resultdownloader.DownloadResult(task);
                }

                (await task.ChangeStateAsync(TaskState.Finished)).ThrowIfError();
                await (task.TryGetOutputHandler<IPlacedTaskOnCompletedHandler>()?.OnPlacedTaskCompleted(task) ?? ValueTask.CompletedTask);
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

            // TODO: fill in TaskObject
            var tk = new ReceivedTask(taskid.Value, new TaskInfo(new("file.mov", 123), info.Input, info.Output, info.Data, TaskPolicy.SameNode, Settings.Guid), true);
            NodeSettings.QueuedTasks.Bindable.Add(tk);
        }
        else taskid = await TaskRegistration.RegisterAsync(info).ConfigureAwait(false);

        return taskid;
    }
    static async Task HandleAsync(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        if (await task.RemoveIfFinished())
            return;


        const int maxattempts = 3;
        NodeGlobalState.Instance.ExecutingTasks.Add(task);
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
            catch (Exception ex)
            {
                task.LogErr(ex);
                task.LogInfo($"Failed to execute task, retrying... ({attempt + 1}/{maxattempts})");
            }
        }

        task.LogErr($"Could not execute this task after {maxattempts} attempts");
        if (task.ExecuteLocally)
        {
            task.LogInfo("Since this task is local, removing it");
            NodeSettings.QueuedTasks.Bindable.Remove(task);
        }
    }


    static async ValueTask<bool> RemoveIfFinished(this ReceivedTask task)
    {
        var state = (await task.GetTaskStateAsync()).ThrowIfError();
        task.LogInfo($"{state.State}/{task.State}");

        if (state.State.IsFinished())
        {
            if (state.State == TaskState.Finished && task.State == TaskState.Output)
            {
                task.LogInfo($"Server task state was set to finished, but the result hasn't been uploaded yet");
                return false;
            }


            task.LogInfo($"Removing");
            NodeSettings.QueuedTasks.Bindable.Remove(task);

            return true;
        }

        return false;
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


    public static T? TryGetInputHandler<T>(this ReceivedTask task) where T : class, ITaskInputHandler => InputHandlers[task.Input.Type] as T;
    public static T? TryGetOutputHandler<T>(this ReceivedTask task) where T : class, ITaskOutputHandler => OutputHandlers[task.Output.Type] as T;

    public static ITaskInputHandler GetInputHandler(this ReceivedTask task) => (ITaskInputHandler) InputHandlers[task.Input.Type];
    public static ITaskOutputHandler GetOutputHandler(this ReceivedTask task) => (ITaskOutputHandler) OutputHandlers[task.Output.Type];
}
