using System.Net;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public class TaskReceiver : ListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Public;
    protected override string? Prefix => "rphtaskexec/launchtask";

    protected override async ValueTask Execute(HttpListenerContext context)
    {
        if (!NodeSettings.AcceptTasks.Value) return;
        if (context.Request.HttpMethod != "POST") return;

        using var response = context.Response;

        var querystr = await new StreamReader(context.Request.InputStream).ReadToEndAsync().ConfigureAwait(false);
        var query = HttpUtility.ParseQueryString(querystr);
        _logger.Info("@rphtaskexec/launchtask received " + HttpUtility.UrlDecode(querystr));

        var values = ReadQueryString(query, "taskid")
            .Next(taskid => ReadQueryString(query, "sign")
            .Next(sign => ReadQueryString(query, "task")
            .Next(task => ReadQueryString(query, "tlhost")
            .Next(host => (taskid, sign, task, host).AsOpResult()))));
        if (!values) return;

        var (taskid, sign, task, host) = values.Result;
        var json = JObject.Parse(task)!;

        var taskinfo = JsonConvert.DeserializeObject<TaskInfo>(task)!;
        _logger.Info($"Received a new task: id: {taskid}; sign: {sign}; data {task}");

        response.StatusCode = (int) await WriteText(response, "{\"ok\":1}");
        response.Close();

        NodeSettings.QueuedTasks.Add(new ReceivedTask(taskid, taskinfo) { HostShard = host });
    }
}
