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

    public static TaskCreationInfo CreateTaskInfo(this WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output, TaskObject tobj)
    {
        var action = TaskList.TryGet(task.TaskAction).ThrowIfNull($"Task action {task.TaskAction} does not exists");

        return new TaskCreationInfo(
            action.Name,
            input,
            output,
            JObject.FromObject(task.TaskData, JsonSettings.LowercaseIgnoreNullS),
            task.Policy,
            tobj
        )
        { SoftwareRequirements = task.SoftwareRequirements };
    }

    public static ValueTask<DbTaskFullState> RegisterTask(this WatchingTask task, string filename, ITaskInputInfo input, TaskObject tobj) => task.RegisterTask(input, task.Output.CreateOutput(task, filename), tobj);
    public static async ValueTask<DbTaskFullState> RegisterTask(this WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output, TaskObject tobj)
    {
        var taskinfo = task.CreateTaskInfo(input, output, tobj);
        var register = await TaskRegistration.TaskRegisterAsync(taskinfo, Settings.SessionId, task).ConfigureAwait(false);
        var newtask = register.ThrowIfError().ThrowIfNull();
        NodeSettings.WatchingTasks.Save(task);

        return newtask;
    }
}
