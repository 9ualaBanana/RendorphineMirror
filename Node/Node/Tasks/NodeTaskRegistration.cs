namespace Node.Tasks;

public static class NodeTaskRegistration
{
    public static ValueTask<OperationResult<string>> RegisterAsync(TaskCreationInfo info, ILoggable? log = null) =>
        TaskRegisterAsync(info, log).Next(t => t.Id.AsOpResult());
    public static async ValueTask<OperationResult<DbTaskFullState>> TaskRegisterAsync(TaskCreationInfo info, ILoggable? log = null)
    {
        if (info.TaskObject is null)
        {
            var input = TaskModels.DeserializeInput(info.Input);
            var handler = input.Type.GetHandler();
            var taskobj = await handler.GetTaskObject(input);
            if (!taskobj) return taskobj.GetResult();

            info.TaskObject = taskobj.Value;
        }

        return await NodeCommon.Tasks.TaskRegistration.TaskRegisterAsync(info, Settings.SessionId, log);
    }
}
