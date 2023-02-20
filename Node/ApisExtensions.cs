namespace Node;

public static class ApisExtensions
{
    static NodeCommon.Apis Apis => Node.Apis.Default;

    public static ValueTask<OperationResult> ShardGet(this ITaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        Apis.ShardGet(task, url, errorDetails, values);
    public static ValueTask<OperationResult> ShardPost(this ITaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        Apis.ShardPost(task, url, errorDetails, values);
    public static ValueTask<OperationResult<T>> ShardGet<T>(this ITaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        Apis.ShardGet<T>(task, url, property, errorDetails, values);
    public static ValueTask<OperationResult<T>> ShardPost<T>(this ITaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        Apis.ShardPost<T>(task, url, property, errorDetails, values);

    public static ValueTask<OperationResult<ServerTaskState>> GetTaskStateAsyncOrThrow(this ITaskApi task) => Apis.GetTaskStateAsyncOrThrow(task);
    public static ValueTask<OperationResult<ServerTaskState?>> GetTaskStateAsync(this ITaskApi task) => Apis.GetTaskStateAsync(task);

    public static ValueTask<OperationResult> ChangeStateAsync(this ITaskApi task, TaskState state) => Apis.ChangeStateAsync(task, state);
    public static async ValueTask<OperationResult> FailTaskAsync(this ITaskApi task, string errorMessage)
    {
        var fail = await Apis.FailTaskAsync(task, errorMessage);
        if (!fail && fail.Message?.Contains("invalid old task state", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return fail;
    }
}