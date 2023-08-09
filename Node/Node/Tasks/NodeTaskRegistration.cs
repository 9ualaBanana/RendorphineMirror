namespace Node.Tasks;

public class NodeTaskRegistration
{
    public required ILifetimeScope ComponentContext { get; init; }

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
}
