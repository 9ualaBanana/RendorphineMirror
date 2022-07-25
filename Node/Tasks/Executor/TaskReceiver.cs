using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Executor;

public class TaskReceiver : IDisposable
{
    readonly HttpListener _httpListener = new();
    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;

    public TaskReceiver(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        _httpListener.Prefixes.Add($"http://*:{PortForwarding.Port}/rphtaskexec/launchtask/");
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
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
                using var _ = GlobalState.TempSetState(new ExecutingTaskNodeState(taskinfo));

                var task = new ReceivedTask(taskid, taskinfo);

                try
                {
                    NodeSettings.ExecutingTasks.Add(task);
                    await TaskHandler.HandleAsync(task, _httpClient, _cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) { Log.Error(ex.ToString()); }
                finally
                {
                    task.LogInfo($"Removing");
                    NodeSettings.ExecutingTasks.Remove(task);
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