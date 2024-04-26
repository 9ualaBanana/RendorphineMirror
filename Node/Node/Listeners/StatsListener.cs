using System.Net;

namespace Node.Listeners;

public class StatsListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.WebServer;
    protected override bool RequiresAuthentication => true;
    protected override string Prefix => "stats";

    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required SystemLoadStoreService LoadService { get; init; }

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

            var result = await LoadService.Load(start, end, stephours);
            return await WriteJson(response, result);
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
