namespace Node;

public class ReconnectTarget
{
    readonly NodeCommon.Apis Api;
    readonly IQueuedTasksStorage QueuedTasksStorage;
    readonly ILogger Logger;

    public ReconnectTarget(NodeCommon.Apis api, IQueuedTasksStorage queuedTasksStorage, ILogger<ReconnectTarget> logger)
    {
        Api = api;
        QueuedTasksStorage = queuedTasksStorage;
        Logger = logger;
    }

    public async Task Execute()
    {
        await cancelTransferredTasks();
        await Api.Api.ApiGet($"{Api.TaskManagerEndpoint}/nodereconnected", "Reconnecting the node", Api.AddSessionId(("guid", Settings.Guid)))
            .ThrowIfError();


        async Task cancelTransferredTasks()
        {
            var shardtasks = QueuedTasksStorage.QueuedTasks.Values.GroupBy(t => t.HostShard);

            foreach (var tasks in shardtasks)
            {
                var shard = tasks.Key;
                var tasksarr = tasks.ToArray();

                if (shard is null)
                {
                    Logger.Error($"Found {tasksarr.Length} tasks without shard being set: {string.Join(", ", tasksarr.Select(t => t.Id))}");
                    continue;
                }

                while (true)
                {
                    var canceltasks = await Api.Api.ApiPost<ImmutableArray<string>>(
                        $"https://{shard}/rphtasklauncher/nodereconnected",
                        "canceltasks",
                        "Getting list of transferred tasks",
                        Api.AddSessionId(
                            ("guid", Settings.Guid),
                            ("taskids", JsonConvert.SerializeObject(tasksarr.Select(t => t.Id)))
                        )
                    ).ThrowIfError();

                    foreach (var cancel in canceltasks)
                        QueuedTasksStorage.QueuedTasks.Remove(cancel);

                    break;
                }

            }
        }
    }
}
