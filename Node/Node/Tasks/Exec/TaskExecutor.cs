using System.Runtime.CompilerServices;
using System.Web;
using Node.Tasks.Exec.Input;
using SixLabors.ImageSharp;

namespace Node.Tasks.Exec;

public class TaskExecutor
{
    static readonly SemaphoreSlim QSPreviewInputSemaphore = new SemaphoreSlim(5);
    static readonly SemaphoreSlim NonQSPreviewInputSemaphore = new SemaphoreSlim(2);
    static readonly SemaphoreSlim ActiveSemaphore = new SemaphoreSlim(1);
    static readonly SemaphoreSlim OutputSemaphore = new SemaphoreSlim(5);

    async Task<FuncDispose> WaitDisposed(SemaphoreSlim semaphore, [CallerArgumentExpression(nameof(semaphore))] string? info = null)
    {
        Logger.LogInformation($"Waiting for {info} lock ({semaphore.CurrentCount})");

        await semaphore.WaitAsync();
        return FuncDispose.Create(semaphore.Release);
    }


    public required Apis Api { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required IIndex<TaskInputType, ITaskInputDownloader> InputDownloaders { get; init; }
    public required IIndex<TaskAction, IPluginActionInfo> Actions { get; init; }
    public required IIndex<TaskOutputType, ITaskUploadHandler> ResultUploaders { get; init; }
    public required TaskExecutorByData Executor { get; init; }
    public required DataDirs Dirs { get; init; }
    public required ILogger<TaskExecutor> Logger { get; init; }


    /// <summary> Ensures all input/active/output types are compatible </summary>
    void CheckCompatibility(ReceivedTask task)
    {
        // TODO::
    }

    public async Task Execute(ReceivedTask task, CancellationToken token)
    {
        Logger.LogInformation($"Task info: {JsonConvert.SerializeObject(task, Formatting.None)}");
        CheckCompatibility(task);

        if (task.State <= TaskState.Input)
        {
            var isqspreview = TaskExecutorByData.GetTaskName(task.Info.Data) == TaskAction.GenerateQSPreview;
            var semaphore = isqspreview ? QSPreviewInputSemaphore : NonQSPreviewInputSemaphore;
            var info = isqspreview ? "qspinput" : "input";

            var downloadedinput = new List<object>();
            foreach (var input in task.Inputs.GroupBy(input => input.Type))
            {
                var inputhandler = InputDownloaders[input.Key];

                using var _ =
                    inputhandler.AllowOutOfOrderDownloads
                    ? new FuncDispose(delegate { })
                    : await WaitDisposed(semaphore, info);

                downloadedinput.AddRange(await inputhandler.MultiDownload(input, task.Info.Object, token));
            }

            task.DownloadedInputs = downloadedinput;


            await Api.ChangeStateAsync(task, TaskState.Active);
            QueuedTasks.QueuedTasks.Save(task);
        }
        else Logger.LogInformation($"Input seems to be already downloaded");

        if (task.State <= TaskState.Active)
        {
            using var _ = await WaitDisposed(ActiveSemaphore);

            var firstaction = Actions[TaskExecutorByData.GetTaskName(task.Info.Data)];
            var inputs = task.DownloadedInputs.ThrowIfNull("No task input downloaded");
            if (inputs.Count == 0) throw new Exception("No task input downloaded");

            object convertInput(object input, int index)
            {
                if (input is IReadOnlyTaskFileList files)
                    return new TaskFileInput(files, Directories.DirCreated(task.FSOutputDirectory(Dirs), index.ToStringInvariant()));

                return input;
            }
            inputs = inputs.Select(convertInput).ToArray();

            task.Result = await Executor.Execute(inputs, (task.Info.Next ?? ImmutableArray<JObject>.Empty).Prepend(task.Info.Data).ToArray());
            await Api.ChangeStateAsync(task, TaskState.Output);
            QueuedTasks.QueuedTasks.Save(task);
        }
        else Logger.LogInformation($"Task execution seems to be already finished");

        if (task.State <= TaskState.Output)
        {
            using var _ = await WaitDisposed(OutputSemaphore);

            var outputhandler = ResultUploaders[task.Output.Type];
            var result = task.Result.ThrowIfNull("No task result");

            await outputhandler.UploadResult(task.Output, task.Inputs.ToArray(), result, token);
            await Api.ChangeStateAsync(task, TaskState.Validation);
        }
        else Logger.LogWarning($"Task result seems to be already uploaded (??????????????)");

        await MaybeNotifyTelegramBotOfTaskCompletion(task, token);
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
        var endpoint = new Uri(new Uri((task.Output as MPlusTaskOutputInfo)?.CustomHost ?? Settings.ServerUrl), "tasks/result").ToString();
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
