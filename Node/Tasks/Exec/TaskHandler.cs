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
    public static async Task InitializePlacedTaskAsync(DbTaskFullState task) =>
        await task.GetInputHandler().InitializePlacedTaskAsync(task);

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


        async ValueTask<bool> check(DbTaskFullState task)
        {
            if (task.State.IsFinished()) return remove();
            const double daysUntilTaskFail = 2;

            try
            {
                // convert unix time from sec to ms for old tasks
                if (task.Registered < 1000000000000)
                    task.Registered *= 1000;

                // fix for old tasks without Registered field being set
                if (task.Registered == 0)
                    task.Registered = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();


                await task.UpdateTaskStateAsync().ThrowIfError();
                if (task.State is TaskState.Failed or TaskState.Canceled)
                    return remove();

                var handler = task.GetOutputHandler();
                var completed = await handler.CheckCompletion(task);
                if (!completed)
                {
                    var latestupdate = task.Times?.OutputTime ?? task.Times?.ActiveTime ?? task.Times?.InputTime ?? DateTimeOffset.FromUnixTimeMilliseconds((long) task.Registered);
                    if ((latestupdate - DateTimeOffset.UtcNow).TotalDays > daysUntilTaskFail)
                    {
                        task.LogInfo($"Canceled due to inactivity over {daysUntilTaskFail} days");
                        (await task.ChangeStateAsync(TaskState.Canceled)).ThrowIfError();

                        return remove();
                    }

                    return false;
                }

                task.LogInfo(task.ToString());
                await handler.OnPlacedTaskCompleted(task);
                (await task.ChangeStateAsync(TaskState.Finished)).ThrowIfError();

                return remove();
            }
            catch (Exception ex) when (
                ex is NodeTaskFailedException
                || ex.Message.Contains("Invalid old task state", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("no task with such ", StringComparison.OrdinalIgnoreCase)
            )
            {
                task.LogErr(ex.Message + ", removing");
                (await task.ChangeStateAsync(TaskState.Canceled)).LogIfError();
                return remove();
            }


            bool remove()
            {
                task.LogInfo($"{task.State}, removing");

                NodeSettings.PlacedTasks.Remove(task);
                foreach (var wtask in NodeSettings.WatchingTasks.Values.ToArray())
                    if (wtask.PlacedNonCompletedTasks.Remove(task.Id))
                        NodeSettings.WatchingTasks.Save(wtask);

                return true;
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

        if (await task.RemoveQueuedIfFinished())
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

        int attempt;
        for (attempt = 0; attempt < maxattempts; attempt++)
        {
            try
            {
                var starttime = DateTimeOffset.Now;
                await TaskList.GetAction(task.Info).Execute(task).ConfigureAwait(false);

                var endtime = DateTimeOffset.Now;
                task.LogInfo($"Task completed in {(endtime - starttime)} and {attempt}/{maxattempts} attempts");

                var taskinfo = await task.GetTaskStateAsync();
                var times = null as TaskTimes;
                if (taskinfo) times = taskinfo.Value.Times;
                NodeSettings.CompletedTasks.Add(new CompletedTask(starttime, endtime, task) { Attempt = attempt, TaskTimes = times });

                task.LogInfo($"Completed, removing");

                task.LogInfo($"Deleting {task.FSInputDirectory()}");
                Directory.Delete(task.FSInputDirectory(), true);

                foreach (var file in task.OutputFiles)
                {
                    if (file.Format == FileFormat.Jpeg) continue;
                    if (!File.Exists(file.Path)) continue;

                    task.LogInfo($"Deleting {file.Path} ({new FileInfo(file.Path).Length / 1024f / 1024 / 1024}G)");
                    File.Delete(file.Path);
                }

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
            NodeSettings.QueuedTasks.Remove(task);
            await task.FailTaskAsync(message).ThrowIfError();
        }
    }
    static async ValueTask<bool> RemoveQueuedIfFinished(this ReceivedTask task)
    {
        if (task is DbTaskFullState) throw new InvalidOperationException("Placed tasks are not permitted");

        var finished = await test();
        if (finished)
        {
            task.LogInfo($"Removing");
            NodeSettings.QueuedTasks.Remove(task);
        }

        return finished;


        async ValueTask<bool> test()
        {
            if (task.ExecuteLocally)
            {
                task.LogInfo($"Local/{task.State}");
                return task.State.IsFinished();
            }

            var stater = await task.GetTaskStateAsync();
            if (!stater)
            {
                if (stater.Message!.Contains("There is no task with such ID.", StringComparison.Ordinal))
                {
                    stater.LogIfError();
                    task.State = TaskState.Failed;

                    return true;
                }

                stater.ThrowIfError();
                return false;
            }

            var state = stater.Value;
            task.LogInfo($"{state.State}/{task.State}");
            if (task.State == TaskState.Queued)
                task.State = state.State;

            if (state.State == TaskState.Finished && task.State == TaskState.Output)
            {
                task.LogInfo($"Server task state was set to finished, but the result hasn't been uploaded yet");
                return false;
            }

            return state.State.IsFinished();
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
