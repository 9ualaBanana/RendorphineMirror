using System.Net;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public class TaskReceiver : ListenerBase
{
    protected override string? Prefix => "rphtaskexec/launchtask";
    protected override bool IsLocal => false;

    protected override async ValueTask Execute(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "POST") return;

        using var response = context.Response;

        var querystr = await new StreamReader(context.Request.InputStream).ReadToEndAsync().ConfigureAwait(false);
        var query = HttpUtility.ParseQueryString(querystr);
        _logger.Info("@rphtaskexec/launchtask received " + HttpUtility.UrlDecode(querystr));

        var values = ReadQueryString(query, "taskid")
            .Next(taskid => ReadQueryString(query, "sign")
            .Next(sign => ReadQueryString(query, "task")
            .Next(task => (taskid, sign, task).AsOpResult())));
        if (!values) return;

        var (taskid, sign, task) = values.Result;
        var json = JObject.Parse(task)!;

        var taskinfo = JsonConvert.DeserializeObject<TaskInfo>(task)!;
        _logger.Info($"Received a new task: id: {taskid}; sign: {sign}; data {task}");

        response.StatusCode = (int) await WriteText(response, "{\"ok\":1}");
        response.Close();

        NodeSettings.QueuedTasks.Bindable.Add(new ReceivedTask(taskid, taskinfo, false));
    }
}
