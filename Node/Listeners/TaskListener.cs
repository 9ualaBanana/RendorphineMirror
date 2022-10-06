using System.Net;
using Newtonsoft.Json;

namespace Node.Listeners;

public class TaskListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local | ListenTypes.Public;
    protected override bool RequiresAuthentication => true;
    protected override string? Prefix => "tasks";


    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        if (path == "pausewatching")
        {
            var taskid = ReadQueryString(context.Request.QueryString, "taskid").ThrowIfError();
            if (!NodeSettings.WatchingTasks.TryGetValue(taskid, out var wtask))
                return await WriteErr(context.Response, "No such task found");

            wtask.IsPaused = !wtask.IsPaused;
            NodeSettings.WatchingTasks.Save(wtask);
            return await WriteJson(context.Response, wtask.AsOpResult());
        }

        if (path == "delwatching")
        {
            var taskid = ReadQueryString(context.Request.QueryString, "taskid").ThrowIfError();
            if (!NodeSettings.WatchingTasks.ContainsKey(taskid))
                return await WriteErr(context.Response, "No such task found");

            NodeSettings.WatchingTasks.Remove(taskid);
            return await WriteSuccess(context.Response);
        }

        return HttpStatusCode.NotFound;
    }
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

            var wt = new WatchingTask(task.Action, task.Data, input, output, task.Policy, task.Version, task.ExecuteLocally);
            wt.StartWatcher();
            NodeSettings.WatchingTasks.Add(wt);

            return await WriteJson(response, wt.Id.AsOpResult()).ConfigureAwait(false);
        }


        return HttpStatusCode.NotFound;
    }
}
