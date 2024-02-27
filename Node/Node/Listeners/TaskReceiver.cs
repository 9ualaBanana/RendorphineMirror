using System.Net;
using System.Web;
using Node.Services.Targets;

namespace Node.Listeners;

public class TaskReceiver : ListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Public;
    protected override string? Prefix => "rphtaskexec/launchtask";

    public required PortsForwardedTarget PortsForwarded { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required Notifier Notifier { get; init; }

    public TaskReceiver(ILogger<TaskReceiver> logger) : base(logger) { }

    protected override async ValueTask Execute(HttpListenerContext context)
    {
        if (!Settings.AcceptTasks.Value) return;
        if (context.Request.HttpMethod != "POST") return;
        using var response = context.Response;

        var querystr = await new StreamReader(context.Request.InputStream).ReadToEndAsync().ConfigureAwait(false);
        var query = HttpUtility.ParseQueryString(querystr);
        Logger.Info("@rphtaskexec/launchtask received " + HttpUtility.UrlDecode(querystr));

        var values = ReadQueryString(query, "taskid")
            .Next(taskid => ReadQueryString(query, "task")
            .Next(task => ReadQueryString(query, "tlhost")
            .Next(host => (taskid, task, host).AsOpResult())));
        if (!values) return;

        var (taskid, task, host) = values.Result;
        var json = JObject.Parse(task)!;

        var taskinfo = JsonConvert.DeserializeObject<TaskInfo>(task)!;
        Logger.Info($"Received a new task: id: {taskid}; data {task}");

        response.StatusCode = (int) await WriteText(response, "{\"ok\":1}");
        response.Close();

        QueuedTasks.QueuedTasks.Add(new ReceivedTask(taskid, taskinfo) { HostShard = host });
        Notifier.Notify($"Received task {taskid}\n ```json\n{JsonConvert.SerializeObject(task, JsonSettings.LowercaseIgnoreNull):n}\n```");
    }
}
