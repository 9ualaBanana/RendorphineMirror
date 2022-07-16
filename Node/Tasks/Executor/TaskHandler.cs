using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Executor;

public static class TaskHandler
{
    public static async Task HandleAsync(ReceivedTask task)
    {
        try
        {
            task.LogInfo($"Started");

            var inputobj = task.Info.DeserializeInput();
            var outputobj = task.Info.DeserializeOutput();
            task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.Indented)}");

            task.LogInfo($"Downloading file...");
            var input = await inputobj.Download(task).ConfigureAwait(false);
            task.LogInfo($"File downloaded to {input}");
            await task.ChangeStateAsync(TaskState.Active);

            var output = await HandleAsyncCore(input, task);
            await task.ChangeStateAsync(TaskState.Output);

            task.LogInfo($"Uploading output file {output} ...");
            await outputobj.Upload(task, output).ConfigureAwait(false);
            task.LogInfo($"File uploaded");
            await task.ChangeStateAsync(TaskState.Finished);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            task.LogErr(ex);
            await task.ChangeStateAsync(TaskState.Canceled);
        }
        catch (Exception ex)
        {
            task.LogErr(ex);
            await task.ChangeStateAsync(TaskState.Failed);
        }
    }

    static async Task<string> HandleAsyncCore(string input, ReceivedTask task)
    {
        var type = task.Info.Data["type"]!.Value<string>()!;
        if (type is null) throw new InvalidOperationException("Task type is null");

        var action = TaskList.TryGet(type);
        if (action is null) throw new InvalidOperationException("Got unknown task type");

        return await action.Execute(task, input).ConfigureAwait(false);
    }
}
