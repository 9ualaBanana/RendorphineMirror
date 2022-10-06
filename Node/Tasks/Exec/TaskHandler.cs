using System.Runtime.Serialization;

namespace Node.Tasks.Exec;

public static class TaskHandler
{
    public static IEnumerable<ITaskInputHandler> InputHandlerList => InputHandlers.Values;
    public static IEnumerable<ITaskOutputHandler> OutputHandlerList => OutputHandlers.Values;

    static readonly Dictionary<TaskInputType, ITaskInputHandler> InputHandlers = new();
    static readonly Dictionary<TaskOutputType, ITaskOutputHandler> OutputHandlers = new();
    static readonly Dictionary<WatchingTaskInputType, Func<WatchingTask, IWatchingTaskInputHandler>> WatchingHandlers = new();


    public static async Task InitializePlacedTasksAsync()
    {
        TaskRegistration.TaskRegistered += task => InitializePlacedTaskAsync(task).Consume();
        await Task.WhenAll(NodeSettings.PlacedTasks.Values.ToArray().Select(InitializePlacedTaskAsync));
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

                if (NodeSettings.QueuedTasks.Count != 0)
                {
                    foreach (var task in NodeSettings.QueuedTasks.Values.ToArray())
                        HandleAsync(task).Consume();

                    continue;
                }
                if (NodeSettings.FailedTasks.Count != 0)
                {
                    foreach (var task in NodeSettings.FailedTasks.Values.ToArray())
                        HandleAsync(task).Consume();

                    continue;
                }
                if (NodeSettings.CanceledTasks.Count != 0)
                {
                    foreach (var task in NodeSettings.CanceledTasks.Values.ToArray())
                        HandleAsync(task).Consume();

                    continue;
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
                foreach (var task in NodeSettings.PlacedTasks.Bindable.Value.ToArray())
                {
                    try { await check(task); }
                    catch (Exception ex) { task.LogErr(ex); }
                }

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
                NodeSettings.PlacedTasks.Remove(task);
            }
            catch (Exception ex) when (ex.Message.Contains("no task with such ", StringComparison.OrdinalIgnoreCase))
            {
                task.LogErr("Placed task does not exists on the server, removing");
                NodeSettings.PlacedTasks.Remove(task);
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid old task state", StringComparison.OrdinalIgnoreCase))
            {
                task.LogErr(ex.Message + ", removing");
                NodeSettings.PlacedTasks.Remove(task);
            }
        }
    }

    public static void StartWatchingTasks()
    {
        foreach (var task in NodeSettings.WatchingTasks.Values)
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
            NodeSettings.QueuedTasks.Add(tk);
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
                NodeSettings.CompletedTasks.Add(new CompletedTask(starttime, endtime, task) { Attempt = attempt });

                task.LogInfo($"Completed, removing");
                NodeSettings.QueuedTasks.Remove(task);
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


        async ValueTask setState(Settings.DatabaseValueDictionary<string, ReceivedTask> newlist, TaskState state, int attempt, string message)
        {
            task.LogInfo($"Task requested to be {state} on attempt ({attempt + 1}/{maxattempts}): {message}");
            newlist.Add(task);
            NodeSettings.QueuedTasks.Remove(task);

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

            NodeSettings.QueuedTasks.Remove(task);
            if (task is DbTaskFullState dbtask)
                NodeSettings.PlacedTasks.Remove(dbtask);
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


    public static ITaskInputHandler GetInputHandler(this ReceivedTask task) => InputHandlers[task.Input.Type];
    public static ITaskOutputHandler GetOutputHandler(this ReceivedTask task) => OutputHandlers[task.Output.Type];
    public static IWatchingTaskInputHandler CreateWatchingHandler(this WatchingTask task) => WatchingHandlers[task.Source.Type](task);
}
