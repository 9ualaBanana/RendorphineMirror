using Node.Profiling;

namespace Node;

public class SystemLoadStoreService
{
    public required INodeLoadStorage NodeLoadStorage { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required ILogger<SystemLoadStoreService> Logger { get; init; }

    public void Start(CancellationToken token)
    {
        NodeLoadStorage.LoadDatabase
            .ExecuteNonQuery($"""
                create table if not exists PartialLoad (
                    key integer primary key unique not null,
                    cpu real not null default 0,
                    gpu real not null default 0,
                    ram integer not null default 0,
                    internetup integer not null default 0,
                    internetdown integer not null default 0
                );
                """);

        StartThreadRepeated(TimeSpan.FromHours(1), ExecuteFull, token);
        StartThreadRepeated(TimeSpan.FromSeconds(1), ExecutePartial, token);
    }

    async Task ExecuteFull()
    {
        var hwload = HardwareLoadSupplier.GetFull();

        var hourBefore = DateTimeOffset.UtcNow.AddHours(-1);

        IReadOnlyCollection<NodeLoadTask> tasks = [
            ..QueuedTasks.QueuedTasks.Values
                .Select(t => new NodeLoadTask(t.Id, t.State, Enum.Parse<TaskAction>(t.FirstAction), t.Times)),

            ..CompletedTasks.CompletedTasks.Values
                .Where(t => t.FinishTime >= hourBefore)
                .Select(t => new NodeLoadTask(t.TaskInfo.Id, t.TaskInfo.State, Enum.Parse<TaskAction>(t.TaskInfo.FirstAction), t.TaskInfo.Times)),
        ];

        var load = new NodeLoad(hwload, tasks.GroupBy(t => t.State).ToDictionary(g => g.Key, g => g.ToArray() as IReadOnlyCollection<NodeLoadTask>));
        Logger.Info($"System load: {JsonConvert.SerializeObject(load, Formatting.None)}");
        NodeLoadStorage.NodeFullLoad.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), load);
    }
    async Task ExecutePartial()
    {
        var hwload = HardwareLoadSupplier.GetPartial();

        NodeLoadStorage.LoadDatabase
            .ExecuteNonQuery($"""
                insert into PartialLoad (key, cpu, gpu, ram, internetup, internetdown) values (
                    {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToStringInvariant()},
                    {hwload.CpuLoad.ToStringInvariant()},
                    {hwload.GpuLoad.ToStringInvariant()},
                    {hwload.FreeRam.ToStringInvariant()},
                    {hwload.InternetUp.ToStringInvariant()},
                    {hwload.InternetDown.ToStringInvariant()}
                )
                """);
    }

    void StartThreadRepeated(TimeSpan repeat, Func<Task> action, CancellationToken token)
    {
        new Thread(async () =>
        {
            while (true)
            {
                if (token.IsCancellationRequested) return;

                try { await action(); }
                catch (Exception ex) { Logger.LogError(ex, ""); }

                await Task.Delay(repeat);
            }
        })
        { IsBackground = true }.Start();
    }
}
