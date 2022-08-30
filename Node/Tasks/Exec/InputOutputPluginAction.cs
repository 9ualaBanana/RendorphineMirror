using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public abstract class InputOutputPluginAction<T> : PluginAction<T>
{
    protected sealed override async Task Execute(ReceivedTask task, T data)
    {
        Directory.CreateDirectory(task.FSOutputDirectory());
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.Indented)}");

        var inputobj = TaskHandler.DeserializeInput(task.Info.Input);
        var outputobj = TaskHandler.DeserializeOutput(task.Info.Output);

        await task.ChangeStateAsync(TaskState.Input);
        task.LogInfo($"Downloading input...");
        var input = await inputobj.Download(task, default).ConfigureAwait(false);
        task.InputFile = input;
        task.LogInfo($"Input downloaded to {input}");

        await task.ChangeStateAsync(TaskState.Active);
        await Execute(task, data, inputobj, outputobj).ConfigureAwait(false);
    }

    // TODO: something about multiple outputs ????
    protected abstract Task Execute(ReceivedTask task, T data, ITaskInput input, ITaskOutput output);
}
