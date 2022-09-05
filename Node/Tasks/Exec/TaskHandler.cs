namespace Node.Tasks.Exec;

public static class TaskHandler
{
    public static IEnumerable<ITaskHandler> HandlerList => Handlers.Values;
    static readonly Dictionary<TaskInputOutputType, ITaskHandler> Handlers = new();


    public static async ValueTask InitializePlacedTasksAsync()
    {
        TaskRegistration.TaskRegistered += task => InitializePlacedTaskAsync(task).AsTask().Consume();

        foreach (var task in NodeSettings.PlacedTasks)
            await InitializePlacedTaskAsync(task);
    }
    public static ValueTask InitializePlacedTaskAsync(DbTaskFullState task) =>
        task.TryGetHandler<IPlacedTaskInitializationHandler>()?.InitializePlacedTaskAsync(task) ?? ValueTask.CompletedTask;

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
            var handler = task.TryGetHandler<IPlacedTaskCompletionCheckHandler>();
            if (handler is null) return;

            try
            {
                var completed = await handler.CheckCompletion(task);
                if (!completed) return;

                task.LogInfo("Completed");
                (await task.ChangeStateAsync(TaskState.Finished)).ThrowIfError();
                task.State = TaskState.Finished;

                await (task.TryGetHandler<IPlacedTaskOnCompletedHandler>()?.OnPlacedTaskCompleted(task) ?? ValueTask.CompletedTask);
            }
            catch (Exception ex) when (ex.Message.Contains("no task with such "))
            {
                task.LogErr("Placed task does not exists on the server, removing");
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
        const int maxattempts = 3;

        var state = await task.GetTaskStateAsync();
        if (state && state.Value.State is (TaskState.Finished or TaskState.Canceled or TaskState.Failed))
        {
            task.LogInfo($"Invalid task state: {state.Value.State}, removing");
            NodeSettings.QueuedTasks.Bindable.Remove(task);

            return;
        }

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



    public static void AddHandler(ITaskHandler handler) => Handlers.Add(handler.Type, handler);
    public static void AddHandlers(params ITaskHandler[] handlers)
    {
        foreach (var handler in handlers)
            AddHandler(handler);
    }

    static T? TryGetHandler<T>(this ReceivedTask task) where T : class, ITaskHandler => Handlers.TryGetValue(task.Output.Type, out var handler) ? handler as T : null;

    public static ITaskInputHandler GetInputHandler(this ReceivedTask task) => (ITaskInputHandler) Handlers[task.Input.Type];
    public static ITaskOutputHandler GetOutputHandler(this ReceivedTask task) => (ITaskOutputHandler) Handlers[task.Output.Type];

    public static ValueTask<string> Download(ReceivedTask task, CancellationToken token = default) =>
        task.GetInputHandler().Download(task, token);

    public static ValueTask UploadResult(ReceivedTask task, string file, string? postfix, CancellationToken token = default) =>
        task.GetOutputHandler().UploadResult(task, file, postfix, token);
}
