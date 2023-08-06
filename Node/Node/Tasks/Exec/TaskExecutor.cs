using System.Runtime.CompilerServices;
using System.Web;
using Node.Tasks.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Node.Tasks.Exec;

public class TaskExecutor
{
    static readonly SemaphoreSlim QSPreviewInputSemaphore = new SemaphoreSlim(3);
    static readonly SemaphoreSlim NonQSPreviewInputSemaphore = new SemaphoreSlim(2);
    static readonly SemaphoreSlim ActiveSemaphore = new SemaphoreSlim(1);
    static readonly SemaphoreSlim OutputSemaphore = new SemaphoreSlim(5);

    static async Task<FuncDispose> WaitDisposed(SemaphoreSlim semaphore, ILoggable task, [CallerArgumentExpression(nameof(semaphore))] string? info = null)
    {
        task.LogInfo($"Waiting for {info} lock ({semaphore.CurrentCount})");

        await semaphore.WaitAsync();
        return FuncDispose.Create(semaphore.Release);
    }


    interface ITaskOutputExecutionContext : ILoggable
    {
        ITaskOutputInfo Output { get; }
        ITaskOutputHandler Handler { get; }

        Task SetValidationAsync();
    }
    record TaskOutputExecutionContext(ReceivedTask Task, ITaskOutputHandler Handler, NodeCommon.Apis Apis) : ApiTaskContextBase(Task, Apis), ITaskOutputExecutionContext
    {
        public ITaskOutputInfo Output => Task.Info.Output;

        public async Task SetValidationAsync() => await ChangeStateAsync(TaskState.Validation);
    }

    static async Task UploadResult(ITaskOutputExecutionContext context)
    {
        // TODO:: delete
        var task = ((TaskOutputExecutionContext) context).Task;

        task.InputFileList.AssertListValid("input");
        task.OutputFileListList.AssertListValid("output");

        context.LogInfo($"Uploading result to {Newtonsoft.Json.JsonConvert.SerializeObject(context.Output, Newtonsoft.Json.Formatting.None)} ... (wh {OutputSemaphore.CurrentCount})");
        context.LogInfo($"Results: {string.Join(" | ", task.OutputFileListList.Select(l => string.Join(", ", l)))}");
        await context.Handler.UploadResult(task, new ReadOnlyTaskFileList(task.OutputFileListList.SelectMany(l => l)) { OutputJson = task.OutputFileListList.Select(t => t.OutputJson).SingleOrDefault(t => t is not null) }).ConfigureAwait(false);
        context.LogInfo($"Result uploaded");

        await context.SetValidationAsync();
    }


    readonly TaskHandlerList TaskHandlerList;
    readonly ILifetimeScope LifetimeScope;
    readonly ILogger Logger;

    public TaskExecutor(TaskHandlerList taskHandlerList, ILifetimeScope lifetimeScope, ILogger<TaskExecutor> logger)
    {
        TaskHandlerList = taskHandlerList;
        LifetimeScope = lifetimeScope;
        Logger = logger;
    }


    public async Task Execute(ReceivedTask task, CancellationToken token)
    {
        using var _logscope = Logger.BeginScope($"Task {task}");
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.None)}");

        using var scope = LifetimeScope.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(task)
                .As<IRegisteredTask>()
                .As<IRegisteredTaskApi>()
                .SingleInstance();

            builder.Register(ctx => new ThrottledProgressSetter(5, new TaskProgressSetter(ctx.Resolve<NodeCommon.Apis>(), task)))
                .As<IProgressSetter>()
                .SingleInstance();
        });


        if (task.State <= TaskState.Input)
        {
            var isqspreview = TaskExecutorByData.GetTaskName(task.Info.Data) == TaskAction.GenerateQSPreview;
            var semaphore = isqspreview ? QSPreviewInputSemaphore : NonQSPreviewInputSemaphore;
            var info = isqspreview ? "qspinput" : "input";
            using var _ = await WaitDisposed(semaphore, task, info);

            task.DownloadedInput = await DownloadInput(scope, task, token);
            await task.ChangeStateAsync(TaskState.Active);
        }
        else task.LogInfo($"Input seems to be already downloaded");

        if (task.State <= TaskState.Active)
        {
            using var _ = await WaitDisposed(ActiveSemaphore, task);

            var input = task.DownloadedInput
                .ThrowIfNull("No task input downloaded")
                .ToObject(TaskHandlerList.GetInputHandler(task).ResultType)
                .ThrowIfNull();

            task.Result = await Execute(scope, task, input);
            await task.ChangeStateAsync(TaskState.Output);
        }
        else task.LogInfo($"Task execution seems to be already finished");

        if (task.State <= TaskState.Output)
        {
            using var _ = await WaitDisposed(OutputSemaphore, task);
            await UploadResult(new TaskOutputExecutionContext(task, TaskHandlerList.GetOutputHandler(task), apis));
        }
        else task.LogWarn($"Task result seems to be already uploaded (??????????????)");

        await MaybeNotifyTelegramBotOfTaskCompletion(task, token);
    }


    async Task<object> DownloadInput(IComponentContext container, ReceivedTask task, CancellationToken token)
    {
        var input = await container.Resolve<TaskInputHandlerByData>()
            .Download(task.Input, task.Info.Object, token);

        // fix jpegs that are rotated using metadata which doesn't do well with some tools
        // TODO: move somewhere else maybe
        foreach (var jpeg in input.Where(f => f.Format == FileFormat.Jpeg).ToArray())
        {
            using var img = Image.Load<Rgba32>(jpeg.Path);
            if (img.Metadata.ExifProfile?.TryGetValue(ExifTag.Orientation, out var exif) == true && exif is not null)
            {
                img.Mutate(ctx => ctx.AutoOrient());
                await img.SaveAsJpegAsync(jpeg.Path, new JpegEncoder() { Quality = 100 });
            }
        }

        return input;
    }
    static async Task<IReadOnlyList<object>> Execute(IComponentContext container, ReceivedTask task, object input) =>
        await container.Resolve<TaskExecutorByData>()
            .Execute(input, (task.Info.Next ?? ImmutableArray<JObject>.Empty).Prepend(task.Info.Data).ToArray());



    static async ValueTask MaybeNotifyTelegramBotOfTaskCompletion(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        if (task.Output.Type != TaskOutputType.MPlus) return;
        if (task.Info.Next is not null) return;
        if (task.FirstAction is not ("VeeeVectorize" or "EsrganUpscale")) return;

        await NotifyTelegramBotOfTaskCompletion(task, cancellationToken);
    }
    static async Task NotifyTelegramBotOfTaskCompletion(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        var endpoint = new Uri(new Uri(Settings.ServerUrl), "tasks/result").ToString();
        var uploadedFiles = string.Join('&', task.UploadedFiles.Cast<MPlusUploadedFileInfo>().Select(fileInfo => $"uploadedfiles={fileInfo.Iid}"));
        var queryString =
            $"id={task.Id}&" +
            $"action={task.Info.FirstTaskType}&" +
            $"{uploadedFiles}&" +
            $"hostshard={HttpUtility.UrlDecode(task.HostShard)}&" +
            $"executor={HttpUtility.UrlDecode(Settings.NodeName)}";

        try { await Api.GlobalClient.PostAsync($"{endpoint}?{queryString}", content: null, cancellationToken); }
        catch (Exception ex) { task.LogErr("Error sending result to Telegram bot: " + ex); }
    }


    abstract record TaskContextBase(ReceivedTask Task) : ILoggable
    {
        public void Log(LogLevel level, string text) => Task.Log(level, text);
    }
    abstract record ApiTaskContextBase(ReceivedTask Task, NodeCommon.Apis Apis) : TaskContextBase(Task)
    {
        protected async Task ChangeStateAsync(TaskState state)
        {
            await Apis.ChangeStateAsync(Task, state).ThrowIfError();
            NodeSettings.QueuedTasks.Save(Task);
        }
    }


    class ThrottledProgressSetter : IProgressSetter
    {
        readonly int ProgressSendDelaySec;
        readonly IProgressSetter Progress;

        public ThrottledProgressSetter(int progressSendDelaySec, IProgressSetter progress)
        {
            ProgressSendDelaySec = progressSendDelaySec;
            Progress = progress;
        }

        DateTime ProgressWriteTime = DateTime.MinValue;
        public void Set(double progress)
        {
            var now = DateTime.Now;
            if (progress >= .98 || ProgressWriteTime < now)
            {
                Progress.Set(progress);
                ProgressWriteTime = DateTime.Now.AddSeconds(ProgressSendDelaySec);
            }
        }
    }

    class TaskProgressSetter : IProgressSetter
    {
        readonly NodeCommon.Apis Api;
        readonly ReceivedTask Task;

        public TaskProgressSetter(NodeCommon.Apis api, ReceivedTask task)
        {
            Api = api;
            Task = task;
        }

        public void Set(double progress)
        {
            Task.Progress = progress;
            Api.SendTaskProgressAsync(Task).Consume();
        }
    }
}
