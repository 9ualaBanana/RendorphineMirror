namespace Node.Tasks.Exec;

public class ReceivedTasksHandler
{
    public required ILifetimeScope Container { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required Apis Api { get; init; }
    public required NodeGlobalState NodeGlobalState { get; init; }
    public required NodeDataDirs Dirs { get; init; }
    public required Notifier Notifier { get; init; }
    public required ILogger<ReceivedTasksHandler> Logger { get; init; }

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

    async Task HandleAsync(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        using var _logscope = Logger.BeginScope($"Task {task.Id}");

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
            Logger.LogInformation($"{task.State}, removing");
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
        Logger.LogInformation($"Execution started");

        var starttime = DateTimeOffset.Now;
        var lastexception = null as Exception;
        int attempt;
        for (attempt = 0; attempt < maxattempts; attempt++)
        {
            try
            {

                using var scope = Container.BeginLifetimeScope(builder =>
                {
                    builder.RegisterInstance(task)
                        .AsSelf()
                        .As<IRegisteredTask>()
                        .As<IRegisteredTaskApi>()
                        .As<IMPlusTask>()
                        .SingleInstance();

                    builder.RegisterType<TaskExecutor>()
                        .SingleInstance();

                    builder.RegisterType<TaskExecutorByData>()
                        .SingleInstance();

                    builder.RegisterType<TaskInputDirectoryProvider>()
                        .As<ITaskInputDirectoryProvider>()
                        .SingleInstance();

                    builder.RegisterType<TaskOutputDirectoryProvider>()
                        .As<ITaskOutputDirectoryProvider>()
                        .SingleInstance();

                    builder.RegisterType<TaskProgressSetter>()
                        .As<ITaskProgressSetter>()
                        .SingleInstance();

                    builder.RegisterDecorator<ITaskProgressSetter>((ctx, parameters, instance) => new ThrottledProgressSetter(TimeSpan.FromSeconds(5), instance));
                });

                Notifier.Notify($"Starting task {task.Id}\n ```json\n{JsonConvert.SerializeObject(task, JsonSettings.LowercaseIgnoreNull):n}\n```");
                var executor = scope.Resolve<TaskExecutor>();
                await executor.Execute(task, cancellationToken).ConfigureAwait(false);

                var endtime = DateTimeOffset.Now;
                Logger.LogInformation($"Task completed in {(endtime - starttime)} and {attempt}/{maxattempts} attempts");

                Notifier.Notify($"Completed task {task.Id}\n ```json\n{JsonConvert.SerializeObject(task, JsonSettings.LowercaseIgnoreNull):n}\n```");
                Logger.LogInformation($"Completed, removing");

                Logger.LogInformation($"Deleting {Dirs.TaskDataDirectory(task.Id)}");
                Directory.Delete(Dirs.TaskDataDirectory(task.Id), true);

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
                Logger.LogError(ex, "");
                Logger.LogInformation($"Failed to execute task, retrying... ({attempt + 1}/{maxattempts})");

                lastexception = ex;
            }
            finally
            {
                CompletedTasks.CompletedTasks.Remove(task.Id);

                var endtime = DateTimeOffset.Now;
                CompletedTasks.CompletedTasks.Add(new CompletedTask(starttime, endtime, task) { Attempt = attempt });
            }
        }

        await fail($"Ran out of attempts: {lastexception?.Message}", $"at {lastexception?.TargetSite}; {lastexception}");


        async ValueTask fail(string errmsg, string fullerrmsg)
        {
            Notifier.Notify($"Failing task {task.Id}\n ```json\n{JsonConvert.SerializeObject(task, JsonSettings.LowercaseIgnoreNull):n}\n```\n\n```\n{fullerrmsg}\n```");
            Logger.LogInformation($"Task was failed ({attempt + 1}/{maxattempts}): {fullerrmsg}");
            await Api.FailTaskAsync(task, errmsg, fullerrmsg).ThrowIfError();

            /*
            Logger.LogInformation($"Deleting {task.FSInputDirectory()} {task.FSOutputDirectory()}");
            Directory.Delete(task.FSInputDirectory(), true);
            Directory.Delete(task.FSOutputDirectory(), true);
            */

            QueuedTasks.QueuedTasks.Remove(task);
        }
        async ValueTask<bool> isFinishedOnServer()
        {
            var state = await Api.GetTaskStateAsync(task).ThrowIfError();
            // Since we are the executor, if state is null, then task state can only be Canceled. Or Finished, but that would be a bug from the task creator node.

            if (state is not null)
            {
                Logger.LogInformation($"R {state.State}/L {task.State}");
                if (task.State == TaskState.Queued)
                    task.State = state.State;
            }

            if (state?.State == TaskState.Finished && task.State == TaskState.Validation)
            {
                Logger.LogError($"Server task state was set to finished, but the result hasn't been uploaded yet (!! bug from the task creator node !!)");
                return false;
            }

            var finished = state is null || state.State.IsFinished();
            if (finished && state is not null) task.State = state.State;

            return finished;
        }
    }


    class TaskProgressSetter : ITaskProgressSetter
    {
        public required Apis Api { get; init; }
        public required IMPlusTask Task { get; init; }

        public void Set(double progress)
        {
            if (Task is ReceivedTask rt) rt.Progress = progress;
            Api.SendTaskProgressAsync(Task).Consume();
        }
    }
    class TaskInputDirectoryProvider : ITaskInputDirectoryProvider
    {
        public required NodeDataDirs Dirs { get; init; }
        public required ReceivedTask Task { get; init; }

        public string InputDirectory => Dirs.TaskInputDirectory(Task.Id);
    }
    class TaskOutputDirectoryProvider : ITaskOutputDirectoryProvider
    {
        public required NodeDataDirs Dirs { get; init; }
        public required ReceivedTask Task { get; init; }

        public string OutputDirectory => Dirs.TaskOutputDirectory(Task.Id);
    }
}
