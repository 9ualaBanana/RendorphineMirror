namespace Node.Tasks.Exec;

public class TaskHandler2
{
    static class NodeSettings { } // to not use accidentally; TODO:: remove whenif this class is moved outside

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    readonly PluginManager PluginManager;
    readonly BindableList<ReceivedTask> QueuedTasks, RunningTasks;
    readonly BindableDictionary<string, CompletedTask> CompletedTasks;

    public TaskHandler2(PluginManager pluginManager, BindableList<ReceivedTask> queuedTasks, BindableList<ReceivedTask> runningTasks, BindableDictionary<string, CompletedTask> completedTasks)
    {
        PluginManager = pluginManager;
        QueuedTasks = queuedTasks;
        RunningTasks = runningTasks;
        CompletedTasks = completedTasks;
    }


    public async Task AddAndRun(ReceivedTask task)
    {
        QueuedTasks.Add(task);
        await Run(task);
    }


    public async Task Run(ReceivedTask task)
    {
        // just in case
        if (task is null)
        {
            var msg = "!! Received a null task to run !!";
            Logger.Error(msg);
            throw new TaskFailedException(msg);
        }

        lock (RunningTasks)
        {
            if (RunningTasks.Contains(task))
            {
                var msg = "!! Received already running task to run !!";
                Logger.Error(msg);
                throw new TaskFailedException(msg);
            }

            RunningTasks.Add(task);
        }
        using var _ = new FuncDispose(() => RunningTasks.Remove(task));


        const int maxattempts = 3;
        task.LogInfo($"Execution started");

        var lastexception = null as Exception;
        int attempt;
        for (attempt = 0; attempt < maxattempts; attempt++)
        {
            try
            {
                var starttime = DateTimeOffset.Now;
                await TaskExecutor.Execute(task, PluginManager).ConfigureAwait(false);

                var endtime = DateTimeOffset.Now;
                task.LogInfo($"Task completed in {endtime - starttime} and {attempt}/{maxattempts} attempts");

                CompletedTasks.Remove(task.Id);
                CompletedTasks.Add(task.Id, new CompletedTask(starttime, endtime, task) { Attempt = attempt });

                task.LogInfo($"Completed, removing");

                task.LogInfo($"Deleting {task.FSDataDirectory()}");
                Directory.Delete(task.FSDataDirectory(), true);

                QueuedTasks.Remove(task);
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

                lastexception = ex;
            }
        }

        var exstr = null as string;
        if (lastexception is not null)
        {
            exstr = $": [{lastexception.GetType().Name}] {lastexception.Message}";
            if (lastexception.TargetSite is not null)
                exstr += $"; at [{lastexception.TargetSite.DeclaringType}] {lastexception.TargetSite}";
        }

        await fail($"Ran out of attempts{exstr}");



        async ValueTask fail(string message)
        {
            task.LogInfo($"Task was failed ({attempt + 1}/{maxattempts}): {message}");
            await task.FailTaskAsync(message).ThrowIfError();

            /*
            task.LogInfo($"Deleting {task.FSInputDirectory()} {task.FSOutputDirectory()}");
            Directory.Delete(task.FSInputDirectory(), true);
            Directory.Delete(task.FSOutputDirectory(), true);
            */

            QueuedTasks.Remove(task);
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
}
