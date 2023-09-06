namespace Node.Common;

public record Apis(Api Api, bool LogErrors = true)
{
    public const string RegistryUrl = "https://t.microstock.plus:7898";
    public static string TaskManagerEndpoint => Api.TaskManagerEndpoint;

    public virtual string SessionId { get; init; } = null!;

    public Apis(Api api, string sessionid, bool logErrors = true) : this(api, logErrors) =>
        SessionId = sessionid;


    public static Apis DefaultWithSessionId(string sid, CancellationToken token = default) => new(Api.Default with { CancellationToken = token }, sid);
    public Apis WithSessionId(string sid) => this with { SessionId = sid };
    public Apis WithNoErrorLog() => this with { LogErrors = false };

    public (string, string)[] AddSessionId(params (string, string)[] values) => Api.AddSessionId(SessionId, values);

    public ValueTask<OperationResult> ShardPost(IRegisteredTaskApi task, string url, string? property, string errorDetails, HttpContent content) =>
        ShardPost<JToken>(task, url, property, errorDetails, content).Next(j => OperationResult.Succ());
    public ValueTask<OperationResult<T>> ShardPost<T>(IRegisteredTaskApi task, string url, string? property, string errorDetails, HttpContent content) =>
        ShardSend(task, url, url => Api.ApiPost<T>(url, property, errorDetails, content));

    public ValueTask<OperationResult> ShardGet(IRegisteredTaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        ShardGet<JToken>(task, url, null, errorDetails, values).Next(j => OperationResult.Succ());
    public ValueTask<OperationResult> ShardPost(IRegisteredTaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        ShardPost<JToken>(task, url, null, errorDetails, values).Next(j => OperationResult.Succ());
    public ValueTask<OperationResult<T>> ShardGet<T>(IRegisteredTaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        ShardSend(task, url, url => Api.ApiGet<T>(url, property, errorDetails, AddSessionId(values)));
    public ValueTask<OperationResult<T>> ShardPost<T>(IRegisteredTaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        ShardSend(task, url, url => Api.ApiPost<T>(url, property, errorDetails, AddSessionId(values)));

    async ValueTask<OperationResult<T>> ShardSend<T>(IRegisteredTaskApi task, string url, Func<string, ValueTask<OperationResult<T>>> func)
    {
        (task as ILoggable)?.LogTrace($"Sending shard request {url}; Shard is {task.HostShard ?? "<null>"}");
        return await ShardSend(task, () => func($"https://{task.HostShard}/rphtasklauncher/{url}"), true);


        async ValueTask<OperationResult<T>> ShardSend(IRegisteredTaskApi task, Func<ValueTask<OperationResult<T>>> func, bool tryDefaultShard)
        {
            if (tryDefaultShard)
            {
                task.HostShard ??= "tasks.microstock.plus";
                tryDefaultShard = false;
            }

            if (task.HostShard is null)
            {
                var host = await UpdateTaskShardAsync(task);
                if (!host) return host;
            }

            var result = await OperationResult.WrapException(func);
            if (result) return result;

            // only if an API error
            if (result.Error is HttpError httpdata)
            {
                if (LogErrors) (task as ILoggable)?.LogErr($"Got error {httpdata.ToString().ReplaceLineEndings(" ")} in ({result})");

                // if nonsuccess, refetch shard host, retry
                if (!httpdata.IsSuccessStatusCode)
                {
                    await Task.Delay(30_000);
                    await UpdateTaskShardAsync(task);
                    return await ShardSend(task, func, tryDefaultShard);
                }

                // "No shard is known for this task. The shard could be restarting, try again in 30 seconds"
                if (httpdata.ErrorCode == ErrorCodes.Error && result.ToString()!.Contains("shard", StringComparison.OrdinalIgnoreCase))
                {
                    var host = await UpdateTaskShardAsync(task);
                    if (!host) return host;

                    await Task.Delay(30_000);
                    return await ShardSend(task, func, tryDefaultShard);
                }
            }

            return result;
        }
    }


    /// <inheritdoc cref="GetTaskShardAsync"/>
    public ValueTask<OperationResult> UpdateTaskShardAsync(IRegisteredTaskApi task) =>
        GetTaskShardAsync(task.Id)
            .Next(s =>
            {
                (task as ILoggable)?.LogTrace($"Shard was updated to {s}");
                task.HostShard = s;
                return OperationResult.Succ();
            });

    /// <summary> Get shard host for a task. Might take a long time to process. Should never return an error, but who knows... </summary>
    public async ValueTask<OperationResult<string>> GetTaskShardAsync(string taskid)
    {
        var shard = await Api.ApiGet<string>($"{TaskManagerEndpoint}/gettaskshard", "host", $"Getting {taskid} task shard", AddSessionId(("taskid", taskid)));
        if (!shard)
        {
            if (shard.Error is not HttpError httpdata)
                return shard;

            if (!httpdata.IsSuccessStatusCode)
            {
                await Task.Delay(30_000);
                return await GetTaskShardAsync(taskid);
            }
        }

        // TODO: fix -72 check
        if (!shard && shard.ToString()!.Contains("-72 error code", StringComparison.Ordinal))
        {
            await Task.Delay(30_000);
            return await GetTaskShardAsync(taskid);
        }

        return shard;
    }

    /// <summary> Maybe get shard host for a task </summary>
    public async ValueTask<OperationResult<string?>> MaybeGetTaskShardAsync(string taskid)
    {
        var shard = await Api.ApiGet<string>($"{TaskManagerEndpoint}/gettaskshard", "host", $"Getting {taskid} task shard", AddSessionId(("taskid", taskid)));

        // TODO: fix -72 check
        if (!shard && shard.ToString()!.Contains("-72 error code", StringComparison.Ordinal))
            return null as string;

        return shard!;
    }


    public async ValueTask<OperationResult<ImmutableArray<string>>> GetShardListAsync() =>
        await Api.ApiGet<ImmutableArray<string>>($"{TaskManagerEndpoint}/getshardlist", "list", "Getting shards list", AddSessionId());
    public async ValueTask<OperationResult<Dictionary<string, string>>> GetTaskShardsAsync(IEnumerable<string> taskids)
    {
        var sel = async (IEnumerable<string> ids) => await Api.ApiGet<Dictionary<string, string>>($"{TaskManagerEndpoint}/gettaskshards", "hosts", "Getting tasks shards",
            AddSessionId(("taskids", JsonConvert.SerializeObject(ids))));

        return await taskids.Chunk(100)
            .Select(sel)
            .AggregateMany();
    }
}