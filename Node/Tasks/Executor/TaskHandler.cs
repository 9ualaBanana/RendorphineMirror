using Newtonsoft.Json.Linq;

namespace Node.Tasks.Executor;

public static class TaskHandler
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

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

                try
                {
                    await HandleAsync(NodeSettings.QueuedTasks.Bindable[index]);
                    index = 0;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.ToString());
                    _logger.Info("Skipping a task");
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
                    try { await TaskRegistration.CheckCompletion(task); }
                    catch (Exception ex) when (ex.Message.Contains("no task with such "))
                    {
                        task.LogErr("Placed task does not exists on the server, removing");
                        NodeSettings.PlacedTasks.Bindable.Remove(task);
                    }
                }

                NodeSettings.PlacedTasks.Save();
                await Task.Delay(30_000);
            }
        })
        { IsBackground = true }.Start();
    }

    public static void StartWatchingTasks()
    {
        foreach (var task in NodeSettings.WatchingTasks.Bindable)
            task.StartWatcher();
    }


    public static ITaskInput DeserializeInput(this ReceivedTask task) => DeserializeInput(task.Info.Input);
    public static ITaskOutput DeserializeOutput(this ReceivedTask task) => DeserializeOutput(task.Info.Output);

    static T GetInputOutputType<T>(JObject json) where T : struct, Enum
    {
        var token = json.Property("type", StringComparison.OrdinalIgnoreCase)?.Value!;

        if (token.Type == JTokenType.Integer)
            return Enum.GetValues<T>()[token.Value<int>()];
        return Enum.Parse<T>(token.Value<string>()!);
    }

    // TODO: refactor these two somehow
    public static ITaskInput DeserializeInput(JObject input) =>
        GetInputOutputType<TaskInputType>(input) switch
        {
            TaskInputType.DownloadLink => new DownloadLinkTaskInput(input.ToObject<DownloadLinkTaskInputInfo>()!),
            TaskInputType.MPlus => new MPlusTaskInput(input.ToObject<MPlusTaskInputInfo>()!),
            TaskInputType.User => new UserTaskInput(input.ToObject<UserTaskInputInfo>()!),
            { } type => throw new NotSupportedException($"Task input type {type} is not supported"),
        };
    public static ITaskOutput DeserializeOutput(JObject output) =>
        GetInputOutputType<TaskOutputType>(output) switch
        {
            TaskOutputType.MPlus => new MPlusTaskOutput(output.ToObject<MPlusTaskOutputInfo>()!),
            TaskOutputType.User => new UserTaskOutput(output.ToObject<UserTaskOutputInfo>()!),
            { } type => throw new NotSupportedException($"Task output type {type} is not supported"),
        };


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
}
