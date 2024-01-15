using System.Net;
using Node.Profiling;

namespace Node.Listeners;

public class StatsListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.WebServer;
    protected override bool RequiresAuthentication => true;
    protected override string Prefix => "stats";

    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required INodeLoadStorage LoadStorage { get; init; }

    public StatsListener(ILogger<StatsListener> logger) : base(logger) { }

    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "getloadbetween")
        {
            var start = ReadQueryLong(request.QueryString, "start").ThrowIfError();
            var end = ReadQueryLong(request.QueryString, "end").ThrowIfError();
            var stephours = ReadQueryLong(request.QueryString, "stephours").ThrowIfError();
            if (stephours < 0) return await WriteErr(response, "Invalid step");

            Dictionary<long, NodeLoad> loadFull() =>
                LoadStorage.NodeFullLoad
                    .GetWhere($"key >= {start.ToStringInvariant()} and key <= {end.ToStringInvariant()}", [])
                    .ToDictionary();

            async Task<Dictionary<long, HardwareLoadPartial>> loadPartial()
            {
                var query = LoadStorage.LoadDatabase
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


            if (stephours == 0)
                return await WriteJson(response, (await loadPartial()).AsOpResult());


            NodeLoad sum(IReadOnlyCollection<NodeLoad> loads)
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
            var data = loadFull()
                .GroupBy(g => g.Key - (g.Key % stepLengthUnix))
                .Select(g => KeyValuePair.Create(g.Key, sum(g.Select(k => k.Value).ToArray())))
                .ToDictionary();


            return await WriteJson(response, data.AsOpResult());
        }

        /*
        if (path == "gettasksat")
        {
            var time = ReadQueryLong(request.QueryString, "time").ThrowIfError();

            var load = LoadStorage.NodeFullLoad
                .GetWhere($"key >= {start.ToStringInvariant()} and key <= {end.ToStringInvariant()}", [])
                .ToDictionary();

            bool filterTask(NodeLoadTask task) =>
                task.Times.Input < time
                && ((task.Times.Failed ?? task.Times.Canceled ?? task.Times.Finished ?? task.Times.Validation ?? task.Times.Output ?? task.Times.Active ?? task.Times.Input) > time);

            return await WriteJson(response, load.Values.SelectMany(t => t.Tasks.Values.SelectMany(t => t).Where(filterTask)).AsOpResult());
        }
        */

        return await base.ExecuteGet(path, context);
    }
}