using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Watching;

public static class WatchingTaskExtensions
{
    static readonly JsonSerializerSettings ConsoleJsonSerializer = new() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.None };


    public static void StartWatcher(this WatchingTask task)
    {
        var action = TaskList.TryGet(task.TaskAction).ThrowIfNull($"Task action {task.TaskAction} does not exists");

        task.Source.FileAdded += async input =>
        {
            task.LogInfo($"New file found: {Serialize(input.InputData)}");

            var output = task.Output.CreateOutput(input.FileName);

            var taskinfo = new TaskCreationInfo(
                action.Type,
                null, // TODO: get version somewhere
                action.Name,
                JObject.FromObject(input.InputData, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", input.InputData.Type.ToString()),
                JObject.FromObject(output, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", output.Type.ToString()),
                JObject.FromObject(task.TaskData, JsonSettings.LowercaseIgnoreNullS),
                task.ExecuteLocally
            );

            var register = await NodeTask.RegisterOrExecute(taskinfo).ConfigureAwait(false);
            var taskid = register.ThrowIfError();

            task.LogInfo($"Created task ID: {taskid}");
        };

        task.LogInfo($"Watcher started; Data: {JsonConvert.SerializeObject(task, Init.IsDebug ? LocalApi.JsonSettingsWithType : new JsonSerializerSettings())}");
        task.Source.StartListening(task);
    }

    static string Serialize<T>(T obj) => obj?.GetType().Name + " " + JsonConvert.SerializeObject(obj, ConsoleJsonSerializer);
}
