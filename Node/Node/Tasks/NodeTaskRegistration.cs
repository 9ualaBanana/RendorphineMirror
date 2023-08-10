namespace Node.Tasks;

public class NodeTaskRegistration
{
    public required ILifetimeScope ComponentContext { get; init; }
    public required IWatchingTasksStorage WatchingTasks { get; init; }

    public ValueTask<OperationResult<string>> RegisterAsync(TaskCreationInfo info, ILoggable? log = null, CancellationToken token = default) =>
        TaskRegisterAsync(info, log, token).Next(t => t.Id.AsOpResult());
    public async ValueTask<OperationResult<DbTaskFullState>> TaskRegisterAsync(TaskCreationInfo info, ILoggable? log = null, CancellationToken token = default)
    {
        if (info.TaskObject is null)
        {
            var input = TaskModels.DeserializeInput(info.Input);
            var handler = ComponentContext.ResolveKeyed<ITaskObjectProvider>(input.Type);
            var taskobj = await handler.GetTaskObject(input, token);
            if (!taskobj) return taskobj.GetResult();

            info.TaskObject = taskobj.Value;
        }

        return await NodeCommon.Tasks.TaskRegistration.TaskRegisterAsync(info, Settings.SessionId, log);
    }


    public Task<DbTaskFullState> RegisterAsync(WatchingTask task, string filename, ITaskInputInfo input, TaskObject tobj) =>
        RegisterAsync(task, input, task.Output.CreateOutput(task, filename), tobj);

    public async Task<DbTaskFullState> RegisterAsync(WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output, TaskObject tobj) =>
        await RegisterAsync(
            task,
            input,
            output,
            tobj,
            JObject.FromObject(task.TaskData, JsonSettings.LowercaseIgnoreNullS)
        );

    public async Task<DbTaskFullState> RegisterAsync(WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output, TaskObject tobj, object data)
    {
        var taskinfo = createTaskInfo(task, input, output, tobj, data);
        var register = await TaskRegisterAsync(taskinfo, task).ConfigureAwait(false);
        var newtask = register.ThrowIfError().ThrowIfNull();
        WatchingTasks.WatchingTasks.Save(task);

        return newtask;


        static TaskCreationInfo createTaskInfo(WatchingTask task, ITaskInputInfo input, ITaskOutputInfo output, TaskObject tobj, object data)
        {
            return new TaskCreationInfo(task.TaskAction, input, output, data, task.Policy, tobj)
            {
                SoftwareRequirements = task.SoftwareRequirements,
            };
        }
    }
}
