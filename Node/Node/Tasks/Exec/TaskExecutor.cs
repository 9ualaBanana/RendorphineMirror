using System.Runtime.CompilerServices;
using System.Web;
using SixLabors.ImageSharp;

namespace Node.Tasks.Exec;

public class TaskExecutor
{
    static readonly SemaphoreSlim QSPreviewInputSemaphore = new SemaphoreSlim(3);
    static readonly SemaphoreSlim NonQSPreviewInputSemaphore = new SemaphoreSlim(2);
    static readonly SemaphoreSlim ActiveSemaphore = new SemaphoreSlim(1);
    static readonly SemaphoreSlim OutputSemaphore = new SemaphoreSlim(5);

    async Task<FuncDispose> WaitDisposed(SemaphoreSlim semaphore, [CallerArgumentExpression(nameof(semaphore))] string? info = null)
    {
        Logger.LogInformation($"Waiting for {info} lock ({semaphore.CurrentCount})");

        await semaphore.WaitAsync();
        return FuncDispose.Create(semaphore.Release);
    }


    readonly ILifetimeScope LifetimeScope;
    readonly ILogger Logger;

    public TaskExecutor(ILifetimeScope lifetimeScope, ILogger<TaskExecutor> logger)
    {
        LifetimeScope = lifetimeScope;
        Logger = logger;
    }


    /// <summary> Ensures all input/active/output types are compatible </summary>
    void CheckCompatibility(ReceivedTask task)
    {
        // TODO::
    }

    public async Task Execute(ReceivedTask task, CancellationToken token)
    {
        using var _logscope = Logger.BeginScope($"Task {task}");
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.None)}");

        CheckCompatibility(task);

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
            using var _ = await WaitDisposed(isqspreview ? QSPreviewInputSemaphore : NonQSPreviewInputSemaphore, info);

            var inputhandler = scope.ResolveKeyed<ITaskInputDownloader>(task.Input.Type);
            task.DownloadedInput = JObject.FromObject(await inputhandler.Download(task.Input, task.Info.Object, token));
            await task.ChangeStateAsync(TaskState.Active);
        }
        else task.LogInfo($"Input seems to be already downloaded");

        if (task.State <= TaskState.Active)
        {
            using var _ = await WaitDisposed(ActiveSemaphore);

            var firstaction = scope.ResolveKeyed<IPluginActionInfo>(TaskExecutorByData.GetTaskName(task.Info.Data));
            var input = task.DownloadedInput
                .ThrowIfNull("No task input downloaded")
                .ToObject(firstaction.InputType)
                .ThrowIfNull();

            var executor = scope.Resolve<TaskExecutorByData>();
            task.Result = JObject.FromObject(await executor.Execute(input, (task.Info.Next ?? ImmutableArray<JObject>.Empty).Prepend(task.Info.Data).ToArray()));

            await task.ChangeStateAsync(TaskState.Output);
        }
        else task.LogInfo($"Task execution seems to be already finished");

        if (task.State <= TaskState.Output)
        {
            using var _ = await WaitDisposed(OutputSemaphore);

            var outputhandler = scope.ResolveKeyed<ITaskUploadHandler>(task.Output.Type);
            var result = task.DownloadedInput
                .ThrowIfNull("No task result")
                .ToObject(outputhandler.ResultType)
                .ThrowIfNull();

            await outputhandler.UploadResult(task.Output, result, token);
            await task.ChangeStateAsync(TaskState.Validation);
        }
        else task.LogWarn($"Task result seems to be already uploaded (??????????????)");

        await MaybeNotifyTelegramBotOfTaskCompletion(task, token);
    }


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
