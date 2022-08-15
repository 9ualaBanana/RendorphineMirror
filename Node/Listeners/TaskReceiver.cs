using System.Net;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public class TaskReceiver : ExecutableListenerBase
{
    protected override string? Prefix => "rphtaskexec/launchtask";
    protected override bool IsLocal => false;

    protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
    {
        var response = context.Response;

        var querystr = await new StreamReader(context.Request.InputStream).ReadToEndAsync().ConfigureAwait(false);
        var query = HttpUtility.ParseQueryString(querystr);
        _logger.Info("@rphtaskexec/launchtask received " + HttpUtility.UrlDecode(querystr));

        var values = ReadQueryString(query, "taskid")
            .Next(taskid => ReadQueryString(query, "sign")
            .Next(sign => ReadQueryString(query, "task")
            .Next(task => (taskid, sign, task).AsOpResult())));
        if (!values) return HttpStatusCode.Forbidden;

        var (taskid, sign, task) = values.Result;
        var json = JObject.Parse(task)!;

        var taskinfo = JsonConvert.DeserializeObject<TaskInfo>(task)!;
        _logger.Info($"Received a new task: id: {taskid}; sign: {sign}; data {task}");

        await WriteSuccess(context.Response);
        TaskHandler.HandleReceivedTask(new ReceivedTask(taskid, taskinfo, false)).Consume();

        return HttpStatusCode.OK;
    }
}
