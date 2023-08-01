using Autofac;

namespace Node.Tasks.Exec;

public class WatchingTaskHandler
{
    readonly TaskHandlerList TaskHandlerList;
    readonly IComponentContext ComponentContext;
    readonly NodeTaskRegistration TaskRegistration;
    readonly IWatchingTasksStorage WatchingTasks;

    public WatchingTaskHandler(TaskHandlerList taskHandlerList, IComponentContext componentContext, NodeTaskRegistration taskRegistration, IWatchingTasksStorage watchingTasks)
    {
        TaskHandlerList = taskHandlerList;
        ComponentContext = componentContext;
        TaskRegistration = taskRegistration;
        WatchingTasks = watchingTasks;
    }

    public IWatchingTaskInputHandler CreateWatchingHandler(WatchingTask task) =>
        TaskHandlerList.WatchingHandlerList[task.Source.Type](task).With(c => ComponentContext.InjectUnsetProperties(c));

    public void StartWatcher(WatchingTask task)
    {
        task.LogInfo($"Watcher started; Data: {JsonConvert.SerializeObject(task, Init.DebugFeatures ? JsonSettings.Typed : new JsonSerializerSettings())}");

        var handler = CreateWatchingHandler(task);
        task.Handler = handler;
        handler.StartListening();
    }

    public ValueTask<DbTaskFullState> RegisterTask(WatchingTask task, string filename, ITaskInputInfo input, TaskObject tobj) =>
        RegisterTask(task, input, task.Output.CreateOutput(task, filename), tobj);

    public async ValueTask<DbTaskFullState> RegisterTask(WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output, TaskObject tobj) =>
        await RegisterTask(
            task,
            input,
            output,
            tobj,
            JObject.FromObject(task.TaskData, JsonSettings.LowercaseIgnoreNullS)
        );

    public async ValueTask<DbTaskFullState> RegisterTask(WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output, TaskObject tobj, object data)
    {
        var taskinfo = CreateTaskInfo(task, input, output, tobj, data);
        var register = await TaskRegistration.TaskRegisterAsync(taskinfo, task).ConfigureAwait(false);
        var newtask = register.ThrowIfError().ThrowIfNull();
        WatchingTasks.WatchingTasks.Save(task);

        return newtask;
    }

    static TaskCreationInfo CreateTaskInfo(WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output, TaskObject tobj, object data)
    {
        return new TaskCreationInfo(task.TaskAction, input, output, data, task.Policy, tobj)
        {
            SoftwareRequirements = task.SoftwareRequirements,
        };
    }
}
