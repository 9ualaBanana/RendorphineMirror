using System.Runtime.CompilerServices;
using System.Web;
using Node.Tasks.Exec.Input;
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


    public required ReceivedTask Task { get; init; }
    public required Apis Api { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required IIndex<TaskInputType, ITaskInputDownloader> InputDownloaders { get; init; }
    public required IIndex<TaskAction, IPluginActionInfo> Actions { get; init; }
    public required IIndex<TaskOutputType, ITaskUploadHandler> ResultUploaders { get; init; }
    public required TaskExecutorByData Executor { get; init; }
    public required ILogger<TaskExecutor> Logger { get; init; }


    /// <summary> Ensures all input/active/output types are compatible </summary>
    void CheckCompatibility(ReceivedTask task)
    {
        // TODO::
    }

    public async Task Execute(CancellationToken token)
    {
        using var _logscope = Logger.BeginScope($"Task {Task.Id}");
        Logger.LogInformation($"Task info: {JsonConvert.SerializeObject(Task, Formatting.None)}");

        CheckCompatibility(Task);

        if (Task.State <= TaskState.Input)
        {
            var isqspreview = TaskExecutorByData.GetTaskName(Task.Info.Data) == TaskAction.GenerateQSPreview;
            var semaphore = isqspreview ? QSPreviewInputSemaphore : NonQSPreviewInputSemaphore;
            var info = isqspreview ? "qspinput" : "input";
            using var _ = await WaitDisposed(isqspreview ? QSPreviewInputSemaphore : NonQSPreviewInputSemaphore, info);

            var inputhandler = InputDownloaders[Task.Input.Type];
            Task.DownloadedInput = await inputhandler.Download(Task.Input, Task.Info.Object, token);
            await Api.ChangeStateAsync(Task, TaskState.Active);
            QueuedTasks.QueuedTasks.Save(Task);
        }
        else Logger.LogInformation($"Input seems to be already downloaded");

        if (Task.State <= TaskState.Active)
        {
            using var _ = await WaitDisposed(ActiveSemaphore);

            var firstaction = Actions[TaskExecutorByData.GetTaskName(Task.Info.Data)];
            var input = Task.DownloadedInput.ThrowIfNull("No task input downloaded");

            if (input is ReadOnlyTaskFileList files)
                input = new TaskFileInput(files, Task.FSOutputDirectory());

            Task.Result = await Executor.Execute(input, (Task.Info.Next ?? ImmutableArray<JObject>.Empty).Prepend(Task.Info.Data).ToArray());
            await Api.ChangeStateAsync(Task, TaskState.Output);
            QueuedTasks.QueuedTasks.Save(Task);
        }
        else Logger.LogInformation($"Task execution seems to be already finished");

        if (Task.State <= TaskState.Output)
        {
            using var _ = await WaitDisposed(OutputSemaphore);

            var outputhandler = ResultUploaders[Task.Output.Type];
            var result = Task.Result.ThrowIfNull("No task result");

            await outputhandler.UploadResult(Task.Output, result, token);
            await Api.ChangeStateAsync(Task, TaskState.Validation);
        }
        else Logger.LogWarning($"Task result seems to be already uploaded (??????????????)");

        await MaybeNotifyTelegramBotOfTaskCompletion(Task, token);
    }


    async ValueTask MaybeNotifyTelegramBotOfTaskCompletion(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        if (task.Output.Type != TaskOutputType.MPlus) return;
        if (task.Info.Next is not null) return;
        if (task.FirstAction is not ("VeeeVectorize" or "EsrganUpscale")) return;

        await NotifyTelegramBotOfTaskCompletion(task, cancellationToken);
    }
    async Task NotifyTelegramBotOfTaskCompletion(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        var endpoint = new Uri(new Uri(Settings.ServerUrl), "tasks/result").ToString();
        var uploadedFiles = string.Join('&', task.UploadedFiles.Cast<MPlusUploadedFileInfo>().Select(fileInfo => $"uploadedfiles={fileInfo.Iid}"));
        var queryString =
            $"id={task.Id}&" +
            $"action={task.Info.FirstTaskType}&" +
            $"{uploadedFiles}&" +
            $"hostshard={HttpUtility.UrlDecode(task.HostShard)}&" +
            $"executor={HttpUtility.UrlDecode(Settings.NodeName)}";

        try { await Api.Api.Client.PostAsync($"{endpoint}?{queryString}", content: null, cancellationToken); }
        catch (Exception ex) { Logger.LogError("Error sending result to Telegram bot: " + ex); }
    }
}
