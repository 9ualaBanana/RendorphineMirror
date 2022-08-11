using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Watching;

public class WatchingTask : IHasTaskId
{
    static readonly JsonSerializerSettings ConsoleJsonSerializer = new() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.None };

    public string Id { get; }
    public readonly IWatchingTaskSource Source;
    public readonly string TaskAction;
    public readonly JObject TaskData;
    public readonly IWatchingTaskOutputInfo Output;
    public readonly bool ExecuteLocally;

    public WatchingTask(IWatchingTaskSource source, string taskaction, JObject taskData, IWatchingTaskOutputInfo output, bool executeLocally, string? id = null)
    {
        Source = source;
        TaskAction = taskaction;
        TaskData = taskData;
        Output = output;
        ExecuteLocally = executeLocally;
        Id = id ?? Guid.NewGuid().ToString();
    }


    public void StartWatcher()
    {
        var action = TaskList.TryGet(TaskAction).ThrowIfNull($"Task action {TaskAction} does not exists");

        Source.FileAdded += async input =>
        {
            this.LogInfo($"New file found: {Serialize(input.InputData)}");

            var output = Output.CreateOutput(input.FileName);

            var taskinfo = new TaskCreationInfo(
                action.Type,
                null, // TODO: get version somewhere
                action.Name,
                JObject.FromObject(input.InputData, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", input.InputData.Type.ToString()),
                JObject.FromObject(output, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", output.Type.ToString()),
                JObject.FromObject(TaskData, JsonSettings.LowercaseIgnoreNullS),
                ExecuteLocally
            );

            var register = await NodeTask.RegisterOrExecute(taskinfo).ConfigureAwait(false);
            var taskid = register.ThrowIfError();

            this.LogInfo($"Created task ID: {taskid}");
        };

        this.LogInfo($"Watcher started; Listening at {Serialize(Source)} for an action {TaskAction} with data {Serialize(TaskData)} and output to {Serialize(Output)}");
        Source.StartListening(this);
    }

    static string Serialize<T>(T obj) => obj?.GetType().Name + " " + JsonConvert.SerializeObject(obj, ConsoleJsonSerializer);
}