using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Watching;

public static class WatchingTaskExtensions
{
    static readonly JsonSerializerSettings ConsoleJsonSerializer = new() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.None };


    public static void StartWatcher(this WatchingTask task)
    {
        task.LogInfo($"Watcher started; Data: {JsonConvert.SerializeObject(task, Init.DebugFeatures ? JsonSettings.Typed : new JsonSerializerSettings())}");

        var handler = task.CreateWatchingHandler();
        task.Handler = handler;
        handler.StartListening();
    }

    public static TaskCreationInfo CreateTaskInfo(this WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output)
    {
        var action = TaskList.TryGet(task.TaskAction).ThrowIfNull($"Task action {task.TaskAction} does not exists");

        return new TaskCreationInfo(
            action.Type,
            task.Version,
            action.Name,
            JObject.FromObject(input, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", input.Type.ToString()),
            JObject.FromObject(output, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", output.Type.ToString()),
            JObject.FromObject(task.TaskData, JsonSettings.LowercaseIgnoreNullS),
            task.Policy
        );
    }

    public static ValueTask<DbTaskFullState> RegisterTask(this WatchingTask task, string filename, ITaskInputInfo input) => task.RegisterTask(input, task.Output.CreateOutput(task, filename));
    public static async ValueTask<DbTaskFullState> RegisterTask(this WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output)
    {
        var taskinfo = task.CreateTaskInfo(input, output);
        var register = await TaskRegistration.TaskRegisterAsync(taskinfo, log: task).ConfigureAwait(false);
        var newtask = register.ThrowIfError();
        NodeSettings.WatchingTasks.Save(task);

        return newtask;
    }
}
