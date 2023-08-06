namespace Node.Tasks;

public class NodeTaskRegistration
{
    public required ILifetimeScope ComponentContext { get; init; }
    public required TaskHandlerList TaskHandlerList { get; init; }

    public ValueTask<OperationResult<string>> RegisterAsync(TaskCreationInfo info, ILoggable? log = null) =>
        TaskRegisterAsync(info, log).Next(t => t.Id.AsOpResult());
    public async ValueTask<OperationResult<DbTaskFullState>> TaskRegisterAsync(TaskCreationInfo info, ILoggable? log = null)
    {
        if (info.TaskObject is null)
        {
            var input = TaskModels.DeserializeInput(info.Input);
            var handler = TaskHandlerList.GetHandler(input.Type);
            var taskobj = await handler.GetTaskObject(ComponentContext, input, default);
            if (!taskobj) return taskobj.GetResult();

            info.TaskObject = taskobj.Value;
        }

        return await NodeCommon.Tasks.TaskRegistration.TaskRegisterAsync(info, Settings.SessionId, log);
    }
}
