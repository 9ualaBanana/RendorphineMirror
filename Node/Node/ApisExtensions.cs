namespace Node;

public static class ApisExtensions
{
    static NodeCommon.Apis Apis => Node.Apis.Default;

    public static ValueTask<OperationResult> ShardGet(this IRegisteredTaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        Apis.ShardGet(task, url, errorDetails, values);
    public static ValueTask<OperationResult> ShardPost(this IRegisteredTaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        Apis.ShardPost(task, url, errorDetails, values);
    public static ValueTask<OperationResult<T>> ShardGet<T>(this IRegisteredTaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        Apis.ShardGet<T>(task, url, property, errorDetails, values);
    public static ValueTask<OperationResult<T>> ShardPost<T>(this IRegisteredTaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        Apis.ShardPost<T>(task, url, property, errorDetails, values);

    public static ValueTask<OperationResult<ServerTaskState>> GetTaskStateAsyncOrThrow(this IRegisteredTaskApi task) => Apis.GetTaskStateAsyncOrThrow(task);
    public static ValueTask<OperationResult<ServerTaskState?>> GetTaskStateAsync(this IRegisteredTaskApi task) => Apis.GetTaskStateAsync(task);

    public static ValueTask<OperationResult> ChangeStateAsync(this IRegisteredTaskApi task, TaskState state) =>
        Apis.ChangeStateAsync(task, state)
        .Next(() =>
        {
            if (task is ReceivedTask rtask)
                NodeSettings.QueuedTasks.Save(rtask);
            return true;
        });
    public static async ValueTask<OperationResult> FailTaskAsync(this IRegisteredTaskApi task, string errmsg, string fullerrmsg)
    {
        var fail = await Apis.FailTaskAsync(task, errmsg, fullerrmsg);
        if (!fail && fail.Message?.Contains("invalid old task state", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return fail;
    }
}