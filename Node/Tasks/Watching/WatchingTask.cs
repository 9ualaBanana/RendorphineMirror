using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Watching;

public class WatchingTask
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    static readonly JsonSerializerSettings ConsoleJsonSerializer = new() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.None };

    public readonly IWatchingTaskSource Source;
    public readonly string TaskAction;
    public readonly JObject TaskData;
    public readonly IWatchingTaskOutputInfo Output;
    public readonly bool ExecuteLocally;

    public WatchingTask(IWatchingTaskSource source, string taskaction, JObject taskData, IWatchingTaskOutputInfo output, bool executeLocally)
    {
        Source = source;
        TaskAction = taskaction;
        TaskData = taskData;
        Output = output;
        ExecuteLocally = executeLocally;
    }


    public void StartWatcher()
    {
        var action = TaskList.TryGet(TaskAction).ThrowIfNull($"Task action {TaskAction} does not exists");

        Source.FileAdded += async input =>
        {
            _logger.Info("A watching task found a new file: {File}", Serialize(input.InputData));

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

            _logger.Info("Task ID: {Id}", taskid);
        };

        _logger.Info("Watching task watcher was started; listening at {Source} for an action {Action} with data {Data} and output to {Output}",
            Serialize(Source), TaskAction, Serialize(TaskData), Serialize(Output));
        Source.StartListening();
    }

    static string Serialize<T>(T obj) => obj?.GetType().Name + " " + JsonConvert.SerializeObject(obj, ConsoleJsonSerializer);
}