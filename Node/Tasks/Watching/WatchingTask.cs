using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Watching;

public class WatchingTask
{
    static readonly JsonSerializerSettings ConsoleJsonSerializer = new() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.None };

    public readonly IWatchingTaskSource Source;
    public readonly string TaskAction;
    public readonly JObject TaskData;
    public readonly IWatchingTaskOutputInfo Output;

    public WatchingTask(IWatchingTaskSource source, string taskaction, JObject taskData, IWatchingTaskOutputInfo output)
    {
        Source = source;
        TaskAction = taskaction;
        TaskData = taskData;
        Output = output;
    }


    public void StartWatcher()
    {
        var action = TaskList.TryGet(TaskAction).ThrowIfNull($"Task action {TaskAction} does not exists");

        Source.FileAdded += async input =>
        {
            Log.Information($"A watching task found a new file: {Serialize(input.InputData)}");

            var output = Output.CreateOutput(input.FileName);

            var taskinfo = new TaskCreationInfo(
                action.Type,
                null, // TODO: get version somewhere
                action.Name,
                JObject.FromObject(input.InputData, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", input.InputData.Type.ToString()),
                JObject.FromObject(output, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", output.Type.ToString()),
                JObject.FromObject(TaskData, JsonSettings.LowercaseIgnoreNullS)
            );

            var register = await NodeTask.RegisterAsync(taskinfo).ConfigureAwait(false);
            var taskid = register.ThrowIfError();
        };

        Log.Information($"Watching task watcher was started; listening at {Serialize(Source)} for an action {TaskAction} with data {Serialize(TaskData)} and output to {Serialize(Output)}");
        Source.StartListening();
    }

    static string Serialize<T>(T obj) => obj?.GetType().Name + " " + JsonConvert.SerializeObject(obj, ConsoleJsonSerializer);
}