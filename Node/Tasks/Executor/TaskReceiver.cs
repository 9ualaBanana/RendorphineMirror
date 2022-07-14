using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Executor;

public class TaskReceiver : IDisposable
{
    readonly HttpListener _httpListener = new();
    public RequestOptions RequestOptions { get; set; }

    public TaskReceiver(RequestOptions? requestOptions = null)
    {
        _httpListener.Prefixes.Add($"http://*:{PortForwarding.Port}/rphtaskexec/launchtask/");
        RequestOptions = requestOptions ?? new();
    }

    public async Task StartAsync()
    {
        _httpListener.Start();

        while (_httpListener.IsListening)
        {
            var context = await _httpListener.GetContextAsync();

            var query = HttpUtility.ParseQueryString(await new StreamReader(context.Request.InputStream).ReadToEndAsync().ConfigureAwait(false));
            var taskid = query["taskid"]!;
            var sign = query["sign"]!;

            var json = JObject.Parse(query["task"]!)!;

            var taskinfo = JsonConvert.DeserializeObject<TaskInfo>(query["task"]!)!;
            Log.Information($"Received a new task: id: {taskid}; sign: {sign}; data {query["task"]}");

            context.Response.OutputStream.Write(Encoding.UTF8.GetBytes("{\"ok\":1}"));
            context.Response.Close();

            var thread = new Thread(async () =>
            {
                ReceivedTask? task = null;
                JObject? taskjson = null;

                try
                {
                    task = new ReceivedTask(taskid, taskinfo);
                    task!.RequestOptions = RequestOptions;
                    taskjson = JObject.FromObject(task);

                    Settings.ActiveTasks.Add(taskjson);
                    await TaskHandler.HandleAsync(task).ConfigureAwait(false);
                }
                catch (Exception ex) { Log.Error(ex.ToString()); }
                finally
                {
                    if (task is not null && taskjson is not null)
                    {
                        task.LogInfo($"Removing");
                        Settings.ActiveTasks.Remove(taskjson);
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
    }

    public void Dispose()
    {
        _httpListener.Close();
        GC.SuppressFinalize(this);
    }
}