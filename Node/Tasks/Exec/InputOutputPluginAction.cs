using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public abstract class InputOutputPluginAction<T> : PluginAction<T>
{
    static readonly SemaphoreSlim InputSemaphore = new SemaphoreSlim(5);
    static readonly SemaphoreSlim TaskWaitHandle = new SemaphoreSlim(1);
    static readonly SemaphoreSlim OutputSemaphore = new SemaphoreSlim(5);

    static async Task<FuncDispose> WaitDisposed(string info, ReceivedTask task, SemaphoreSlim semaphore)
    {
        if (semaphore.CurrentCount == 0)
            task.LogInfo($"Waiting for the {info} handle: {semaphore.CurrentCount}");

        await semaphore.WaitAsync();
        return FuncDispose.Create(semaphore.Release);
    }

    protected sealed override async Task Execute(ReceivedTask task, T data)
    {
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.None)}");

        if (task.State <= TaskState.Input)
        {
            using var _ = await WaitDisposed("input", task, InputSemaphore);

            task.LogInfo($"Downloading input...");
            await task.GetInputHandler().Download(task).ConfigureAwait(false);
            task.LogInfo($"Input downloaded from {Newtonsoft.Json.JsonConvert.SerializeObject(task.Info.Input, Newtonsoft.Json.Formatting.None)}");

            await task.ChangeStateAsync(TaskState.Active);
            NodeSettings.QueuedTasks.Save(task);
        }
        else task.LogInfo($"Input seems to be already downloaded");

        if (task.State <= TaskState.Active)
        {
            using var _ = await WaitDisposed("active", task, TaskWaitHandle);

            task.LogInfo($"Checking input files...");
            task.GetAction().InputRequirements.Check(task).ThrowIfError();

            task.LogInfo($"Executing task...");
            await ExecuteImpl(task, data).ConfigureAwait(false);
            task.LogInfo($"Task executed");

            await task.ChangeStateAsync(TaskState.Output);
            NodeSettings.QueuedTasks.Save(task);
        }
        else task.LogInfo($"Task execution seems to be already finished");

        if (task.State <= TaskState.Output)
        {
            using var _ = await WaitDisposed("output", task, OutputSemaphore);

            task.LogInfo($"Uploading result to {Newtonsoft.Json.JsonConvert.SerializeObject(task.Info.Output, Newtonsoft.Json.Formatting.None)} ...");
            await task.GetOutputHandler().UploadResult(task).ConfigureAwait(false);
            task.LogInfo($"Result uploaded");

            NodeSettings.QueuedTasks.Save(task);
        }
        else task.LogWarn($"Task result seems to be already uploaded (??????????????)");

        await NotifyReepoOfTaskCompletion(task);
    }


    static async Task NotifyReepoOfTaskCompletion(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        if (task.ExecuteLocally) return;

        var queryString = $"taskid={task.Id}&nodename={Settings.NodeName}";
        try { await Api.Client.PostAsync($"{Settings.ServerUrl}/tasks/result_preview?{queryString}", null, cancellationToken); }
        catch (Exception ex) { task.LogErr("Error sending result to reepo: " + ex); }
    }

    protected abstract Task ExecuteImpl(ReceivedTask task, T data);
}
