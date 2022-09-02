using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public abstract class InputOutputPluginAction<T> : PluginAction<T>
{
    protected sealed override async Task Execute(ReceivedTask task, T data)
    {
        Directory.CreateDirectory(task.FSOutputDirectory());
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.Indented)}");

        await task.ChangeStateAsync(TaskState.Input);
        task.LogInfo($"Downloading input...");
        var input = await TaskInputOutput.Download(task, default).ConfigureAwait(false);
        task.InputFile = input;
        task.LogInfo($"Input downloaded to {input}");

        await task.ChangeStateAsync(TaskState.Active);
        await ExecuteImpl(task, data).ConfigureAwait(false);

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
