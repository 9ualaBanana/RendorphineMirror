using System.Net;
using Newtonsoft.Json;

namespace Node.Listeners;

public class TaskListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local | ListenTypes.Public;
    protected override bool RequiresAuthentication => true;
    protected override string? Prefix => "tasks";


    protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "start")
        {
            var task = new JsonSerializer().Deserialize<TaskCreationInfo>(new JsonTextReader(new StreamReader(request.InputStream)))!;
            var taskid = await TaskHandler.RegisterOrExecute(task);

            return await WriteJson(response, taskid).ConfigureAwait(false);
        }
        if (path == "startwatching")
        {
            var task = new JsonSerializer().Deserialize<TaskCreationInfo>(new JsonTextReader(new StreamReader(request.InputStream)))!;
            var input = TaskModels.DeserializeWatchingInput(task.Input);
            var output = TaskModels.DeserializeWatchingOutput(task.Output);

            var wt = new WatchingTask(input, task.Action, task.Data, output, task.Policy, task.Version, task.ExecuteLocally);
            wt.StartWatcher();
            NodeSettings.WatchingTasks.Add(wt);

            return await WriteJson(response, wt.Id.AsOpResult()).ConfigureAwait(false);
        }


        return HttpStatusCode.NotFound;
    }
}
