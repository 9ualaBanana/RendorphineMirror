namespace Node.Services.Targets;

public class ReconnectTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required AuthenticatedTarget Authenticated { get; init; }
    public required Apis Api { get; init; }
    public required IQueuedTasksStorage QueuedTasksStorage { get; init; }
    public required ILogger<ReconnectTarget> Logger { get; init; }

    public async Task ExecuteAsync()
    {
        Logger.LogInformation("Reconnecting to M+");

        await cancelTransferredTasks();
        await Api.Api.ApiGet($"{Apis.TaskManagerEndpoint}/nodereconnected", "Reconnecting the node", Api.AddSessionId(("guid", Settings.Guid)))
            .ThrowIfError();

        Logger.LogInformation("Reconnected");


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
