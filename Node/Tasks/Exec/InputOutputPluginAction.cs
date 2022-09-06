using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public abstract class InputOutputPluginAction<T> : PluginAction<T>
{
    protected sealed override async Task Execute(ReceivedTask task, T data)
    {
        Directory.CreateDirectory(task.FSOutputDirectory());
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.Indented)}");

        if (task.State <= TaskState.Input || task.InputFile is null)
        {
            await task.ChangeStateAsync(TaskState.Input);
            task.LogInfo($"Downloading input...");
            var input = await TaskHandler.Download(task, default).ConfigureAwait(false);
            task.InputFile = input;
            task.LogInfo($"Input downloaded to {input}");

            NodeSettings.QueuedTasks.Save();
        }
        else task.LogInfo($"Input seems to be already downloaded to {task.InputFile}");

        if (task.State <= TaskState.Output)
        {
            await task.ChangeStateAsync(TaskState.Active);
            NodeSettings.QueuedTasks.Save();

            await ExecuteImpl(task, data).ConfigureAwait(false);
            NodeSettings.QueuedTasks.Save();
        }

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
