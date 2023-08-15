using System.Net;

namespace Node.Listeners;

public class TaskListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local | ListenTypes.Public;
    protected override bool RequiresAuthentication => true;
    protected override string? Prefix => "tasks";

    readonly WatchingTaskHandler WatchingTaskHandler;
    readonly IWatchingTasksStorage WatchingTasks;
    readonly NodeTaskRegistration TaskRegistration;

    public TaskListener(WatchingTaskHandler watchingTaskHandler, IWatchingTasksStorage watchingTasks, NodeTaskRegistration taskRegistration, ILogger<TaskListener> logger) : base(logger)
    {
        WatchingTaskHandler = watchingTaskHandler;
        WatchingTasks = watchingTasks;
        TaskRegistration = taskRegistration;
    }

    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        if (path == "pausewatching")
        {
            var taskid = ReadQueryString(context.Request.QueryString, "taskid").ThrowIfError();
            if (!WatchingTasks.WatchingTasks.TryGetValue(taskid, out var wtask))
                return await WriteErr(context.Response, "No such task found");

            wtask.IsPaused = !wtask.IsPaused;
            WatchingTasks.WatchingTasks.Save(wtask);
            return await WriteJson(context.Response, wtask.AsOpResult());
        }

        if (path == "delwatching")
        {
            var taskid = ReadQueryString(context.Request.QueryString, "taskid").ThrowIfError();
            if (!WatchingTasks.WatchingTasks.ContainsKey(taskid))
                return await WriteErr(context.Response, "No such task found");

            WatchingTasks.WatchingTasks.Remove(taskid);
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
            var taskid = await TaskRegistration.TaskRegisterAsync(task);

            return await WriteJson(response, taskid.Next(task => task.Id.AsOpResult())).ConfigureAwait(false);
        }
        if (path == "startwatching")
        {
            var task = new JsonSerializer().Deserialize<TaskCreationInfo>(new JsonTextReader(new StreamReader(request.InputStream)))!;
            var input = TaskModels.DeserializeWatchingInput(task.Input);
            var output = TaskModels.DeserializeWatchingOutput(task.Output);

            var wt = new WatchingTask(task.Action, task.Data, input, output, task.Policy) { SoftwareRequirements = task.SoftwareRequirements };
            WatchingTaskHandler.StartWatcher(wt);
            WatchingTasks.WatchingTasks.Add(wt);

            return await WriteJson(response, wt.Id.AsOpResult()).ConfigureAwait(false);
        }


        return HttpStatusCode.NotFound;
    }
}
