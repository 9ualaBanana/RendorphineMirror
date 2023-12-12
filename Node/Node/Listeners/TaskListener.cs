using System.Net;
using Node.Tasks.Exec.Input;
using Node.Tasks.Models.ExecInfo;

namespace Node.Listeners;

public class TaskListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local | ListenTypes.Public;
    protected override bool RequiresAuthentication => true;
    protected override string? Prefix => "tasks";

    public required WatchingTasksHandler WatchingTasksHandler { get; init; }
    public required IWatchingTasksStorage WatchingTasks { get; init; }
    public required NodeTaskRegistration TaskRegistration { get; init; }
    public required DataDirs Dirs { get; init; }
    public required ILifetimeScope Container { get; init; }

    public TaskListener(ILogger<TaskListener> logger) : base(logger) { }

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

    public record QSPreviewTaskExecutionInfo(IReadOnlyList<string> Input, QSPreviewInfo Data);
    class TaskProgressSetter : ITaskProgressSetter
    {
        public required ILogger<TaskProgressSetter> Logger { get; init; }

        public void Set(double progress) => Logger.Trace($"Task progress: {progress}");
    }
    protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "executeqsp")
        {
            using var scope = Container.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<TaskExecutorByData>()
                    .SingleInstance();

                builder.RegisterType<TaskProgressSetter>()
                    .As<ITaskProgressSetter>()
                    .SingleInstance();

                builder.RegisterDecorator<ITaskProgressSetter>((ctx, parameters, instance) => new ThrottledProgressSetter(TimeSpan.FromSeconds(5), instance));
            });

            var info = new JsonSerializer().Deserialize<QSPreviewTaskExecutionInfo>(new JsonTextReader(new StreamReader(request.InputStream)))!;

            var executor = scope.Resolve<TaskExecutorByData>();
            var input = new TaskFileInput(new ReadOnlyTaskFileList(info.Input.Select(FileWithFormat.FromFile)), ReceivedTask.FSOutputDirectory(Dirs, $"local_{Guid.NewGuid()}"));
            var data = JObject.FromObject(info.Data).WithProperty("type", TaskAction.GenerateQSPreview.ToString());

            var result = await executor.Execute(new[] { input }, new[] { data });
            return await WriteJson(response, result.AsOpResult()).ConfigureAwait(false);
        }

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
            WatchingTasks.WatchingTasks.Add(wt);
            WatchingTasksHandler.StartWatchingTask(wt);

            return await WriteJson(response, wt.Id.AsOpResult()).ConfigureAwait(false);
        }


        return HttpStatusCode.NotFound;
    }
}
