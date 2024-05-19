using System.Collections;
using Node.Profiling;

namespace Node;

public class SystemLoadStoreService
{
    static readonly TimeSpan StateSendInterval = TimeSpan.FromMinutes(5);
    public required INodeLoadStorage NodeLoadStorage { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required Init Init { get; init; }
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
        StartThreadRepeated(TimeSpan.FromSeconds(10), ExecutePartial, token);
        StartThreadRepeated(StateSendInterval, Send, token);
    }

    async Task Send(CancellationToken token)
    {
        var url = "https://nodes.renderfin.com/";

        var start = DateTimeOffset.UtcNow.Subtract(StateSendInterval).ToUnixTimeMilliseconds();
        var end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var content = new
        {
            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Info = new
            {
                Settings.NodeName,
                Environment.UserName,
                Environment.MachineName,
                Init.Version,
                Ip = (await PortForwarding.GetPublicIPAsync(token)).ToString(),
                UPnpPort = await PortForwarding.GetPublicFacingPort(Settings.Instance, token),
                UPnpServerPort = await PortForwarding.GetPublicFacingPort(Settings.Instance, token),
            },
            Partial = await LoadPartial(start, end),
            Load = GetFull(StateSendInterval),
        };
        var contentstr = JsonConvert.SerializeObject(content, Formatting.None);

        var token2 = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token);
        using var response = await Api.GlobalClient.PostAsync(url, new StringContent(contentstr) { Headers = { ContentType = new("application/json") } }, token2.Token);
        await Api.LogRequest(response, null, "Sending node load to nodelist", Logger, token);
    }

    async Task<Dictionary<long, HardwareLoadPartial>> LoadPartial(long start, long end)
    {
        var query = NodeLoadStorage.LoadDatabase
            .ExecuteQuery($"select * from PartialLoad where key >= {start.ToStringInvariant()} and key <= {end.ToStringInvariant()}");

        var loads = new Dictionary<long, HardwareLoadPartial>();
        while (await query.ReadAsync())
        {
            // using Convert.To*** instead of query.Get*** because of stupid invalid cast errors

            var key = Convert.ToInt64(query.GetValue(query.GetOrdinal("key")), CultureInfo.InvariantCulture);
            var cpu = Convert.ToDouble(query.GetValue(query.GetOrdinal("cpu")), CultureInfo.InvariantCulture);
            var gpu = Convert.ToDouble(query.GetValue(query.GetOrdinal("gpu")), CultureInfo.InvariantCulture);
            var ram = Convert.ToInt64(query.GetValue(query.GetOrdinal("ram")), CultureInfo.InvariantCulture);
            var internetup = Convert.ToInt64(query.GetValue(query.GetOrdinal("internetup")), CultureInfo.InvariantCulture);
            var internetdown = Convert.ToInt64(query.GetValue(query.GetOrdinal("internetdown")), CultureInfo.InvariantCulture);

            loads.Add(key, new HardwareLoadPartial(cpu, gpu, ram, internetup, internetdown));
        }

        return loads;
    }
    Dictionary<long, NodeLoad> LoadFull(long start, long end, long stephours)
    {
        static NodeLoad sum(IReadOnlyCollection<NodeLoad> loads)
        {
            var hwloadpartial = new HardwareLoadPartial(
                loads.Average(l => l.HardwareLoad.Load.CpuLoad),
                loads.Average(l => l.HardwareLoad.Load.GpuLoad),
                (long) loads.Average(l => l.HardwareLoad.Load.FreeRam),
                (long) loads.Average(l => l.HardwareLoad.Load.InternetDown),
                (long) loads.Average(l => l.HardwareLoad.Load.InternetUp)
            );
            var hwloaddrives = loads
                .SelectMany(l => l.HardwareLoad.Drives)
                .GroupBy(d => d.Key)
                .Select(d => KeyValuePair.Create(d.Key, new HardwareLoadDrive((long) d.Average(d => d.Value.FreeSpace))))
                .ToDictionary();

            var hwload = new HardwareLoad(hwloadpartial, hwloaddrives);
            var tasks = loads
                .SelectMany(l => l.Tasks ?? [])
                .GroupBy(k => k.Key)
                .Select(k => KeyValuePair.Create(k.Key, k.SelectMany(t => t.Value).ToArray() as IReadOnlyCollection<NodeLoadTask>))
                .ToDictionary();

            return new NodeLoad(hwload, tasks);
        }

        var stepLengthUnix = (DateTimeOffset.FromUnixTimeMilliseconds(0) + TimeSpan.FromHours(stephours)).ToUnixTimeMilliseconds();
        return NodeLoadStorage.NodeFullLoad
            .GetWhere($"key >= {start.ToStringInvariant()} and key <= {end.ToStringInvariant()}", [])
            .ToDictionary()
            .GroupBy(g => g.Key - (g.Key % stepLengthUnix))
            .Select(g => KeyValuePair.Create(g.Key, sum(g.Select(k => k.Value).ToArray())))
            .ToDictionary();
    }
    public async Task<OperationResult<IDictionary>> Load(long start, long end, long stephours)
    {
        if (stephours < 0)
            return OperationResult.Err("Invalid step");

        if (stephours == 0)
            return (await LoadPartial(start, end) as IDictionary).AsOpResult();

        return (LoadFull(start, end, stephours) as IDictionary).AsOpResult();
    }

    NodeLoad GetFull(TimeSpan? diff = null)
    {
        var hwload = HardwareLoadSupplier.GetFull();
        var hourBefore = DateTimeOffset.UtcNow - (diff ?? TimeSpan.FromHours(1));

        IReadOnlyCollection<NodeLoadTask> tasks = [
            ..QueuedTasks.QueuedTasks.Values
                .Select(t => new NodeLoadTask(t.Id, t.State, Enum.Parse<TaskAction>(t.FirstAction), t.Times)),

            ..CompletedTasks.CompletedTasks.Values
                .Where(t => t.FinishTime >= hourBefore)
                .Select(t => new NodeLoadTask(t.TaskInfo.Id, t.TaskInfo.State, Enum.Parse<TaskAction>(t.TaskInfo.FirstAction), t.TaskInfo.Times)),
        ];

        return new NodeLoad(hwload, tasks.GroupBy(t => t.State).ToDictionary(g => g.Key, g => g.ToArray() as IReadOnlyCollection<NodeLoadTask>));
    }
    async Task ExecuteFull()
    {
        var load = GetFull();
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

    void StartThreadRepeated(TimeSpan repeat, Func<Task> action, CancellationToken token) => StartThreadRepeated(repeat, _ => action(), token);
    void StartThreadRepeated(TimeSpan repeat, Func<CancellationToken, Task> action, CancellationToken token)
    {
        new Thread(async () =>
        {
            while (true)
            {
                if (token.IsCancellationRequested) return;

                try { await action(token); }
                catch (Exception ex) { Logger.LogError(ex, ""); }

                await Task.Delay(repeat);
            }
        })
        { IsBackground = true }.Start();
    }
}
