using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public abstract class InputOutputPluginAction<T> : PluginAction<T>
{
    protected sealed override async Task Execute(ReceivedTask task, T data)
    {
        Directory.CreateDirectory(task.FSOutputDirectory());
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.Indented)}");

        if (task.State <= TaskState.Queued)
        {
            await task.ChangeStateAsync(TaskState.Input);
            NodeSettings.QueuedTasks.Save();
        }

        if (task.State <= TaskState.Input || task.InputFile is null)
        {
            task.LogInfo($"Downloading input...");
            var input = await task.GetInputHandler().Download(task).ConfigureAwait(false);
            task.InputFile = input;
            task.LogInfo($"Input downloaded to {input}");

            await task.ChangeStateAsync(TaskState.Active);
            NodeSettings.QueuedTasks.Save();
        }
        else task.LogInfo($"Input seems to be already downloaded to {task.InputFile}");

        if (task.State <= TaskState.Active)
        {
            task.LogInfo($"Executing task...");
            await ExecuteImpl(task, data).ConfigureAwait(false);
            task.LogInfo($"Task executed");

            await task.ChangeStateAsync(TaskState.Output);
            NodeSettings.QueuedTasks.Save();
        }
        else task.LogInfo($"Task execution seems to be already finished");

        if (task.State <= TaskState.Output)
        {
            task.LogInfo($"Uploading result to {Newtonsoft.Json.JsonConvert.SerializeObject(task.Info.Output, Newtonsoft.Json.Formatting.None)} ...");
            await task.GetOutputHandler().UploadResult(task).ConfigureAwait(false);
            task.LogInfo($"Result uploaded");

            NodeSettings.QueuedTasks.Save();
        }
        else task.LogInfo($"Task result seems to be already uploaded (??????????????)");

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
