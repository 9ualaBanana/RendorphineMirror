using System.Runtime.CompilerServices;
using System.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Node.Tasks.Exec;

public static class TaskExecutor
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



    interface ITaskInputExecutionContext : ILoggable
    {
        ITaskInputInfo Input { get; }
        ITaskInputHandler Handler { get; }

        Task SetActiveAsync(ReadOnlyTaskFileList files);
    }
    record TaskInputExecutionContext(ReceivedTask Task, NodeCommon.Apis Apis) : ApiTaskContextBase(Task, Apis), ITaskInputExecutionContext
    {
        public ITaskInputInfo Input => Task.Info.Input;
        public ITaskInputHandler Handler => Task.Info.Input.Type.GetHandler();

        public async Task SetActiveAsync(ReadOnlyTaskFileList files)
        {
            Task.InputFileList = files;
            await ChangeStateAsync(TaskState.Active);
        }
    }

    static async Task DownloadInput(ITaskInputExecutionContext context)
    {
        // TODO:: delete
        var task = ((TaskInputExecutionContext) context).Task;

        context.LogInfo($"Downloading input");
        var inputfiles = await context.Handler.Download(task).ConfigureAwait(false);
        context.LogInfo($"Input downloaded from {Newtonsoft.Json.JsonConvert.SerializeObject(context.Input, Newtonsoft.Json.Formatting.None)}");

        // fix jpegs that are rotated using metadata which doesn't do well with some tools
        foreach (var jpeg in inputfiles.Where(f => f.Format == FileFormat.Jpeg).ToArray())
        {
            using var img = Image.Load<Rgba32>(jpeg.Path);
            if (img.Metadata.ExifProfile?.TryGetValue(ExifTag.Orientation, out var exif) == true && exif is not null)
            {
                img.Mutate(ctx => ctx.AutoOrient());
                await img.SaveAsJpegAsync(jpeg.Path, new JpegEncoder() { Quality = 100 });
            }
        }

        await context.SetActiveAsync(inputfiles);
    }



    interface ITaskExecutionExecutionContext : ILoggable
    {
        IReadOnlyCollection<JObject> Datas { get; }
        IReadOnlyCollection<Plugin> Plugins { get; }

        Task SetOutputAsync(IReadOnlyTaskFileListList files);
        string NewTaskResultDirectory(string addition);
    }
    record TaskExecutionExecutionContext(ReceivedTask Task, NodeCommon.Apis Apis, IReadOnlyCollection<Plugin> Plugins) : ApiTaskContextBase(Task, Apis), ITaskExecutionExecutionContext
    {
        public IReadOnlyCollection<JObject> Datas { get; } = (Task.Info.Next ?? ImmutableArray<JObject>.Empty).Prepend(Task.Info.Data).ToArray();

        public async Task SetOutputAsync(IReadOnlyTaskFileListList files)
        {
            Task.OutputFileListList = files;
            await ChangeStateAsync(TaskState.Output);
        }
        public string NewTaskResultDirectory(string add) => Task.FSOutputDirectory(add);
    }

    static async Task Execute(ITaskExecutionExecutionContext context, ReadOnlyTaskFileList? inputfiles)
    {
        // TODO:: delete
        var task = ((TaskExecutionExecutionContext) context).Task;

        var econtext = new TaskExecutionContext(task, context.Plugins, new MPlusApiService(task.Id, Apis.Default.SessionId, Api.Default));

        CheckFileList(inputfiles, "input");

        var outputs = null as TaskFileListList;
        var index = 0;
        foreach (var data in context.Datas)
        {
            var prevoutput = outputs ?? new TaskFileListList("/does/not/exists") { inputfiles };
            outputs = new TaskFileListList(context.NewTaskResultDirectory(index.ToString()));

            foreach (var input in prevoutput)
            {
                outputs.InputFiles = input;
                await TaskList.GetAction(TaskInfo.GetTaskType(data)).Execute(
                    new TaskExecutionContextSubtaskOverlay(index, context.Datas.Count, econtext),
                    new TaskFiles(input, outputs),
                    data
                );
            }

            index++;
        }

        outputs.ThrowIfNull("No task result (what?)");
        econtext.SetProgress(1);
        await context.SetOutputAsync(outputs);
    }



    interface ITaskOutputExecutionContext : ILoggable
    {
        ITaskOutputInfo Output { get; }
        ITaskOutputHandler Handler { get; }

        Task SetValidationAsync();
    }
    record TaskOutputExecutionContext(ReceivedTask Task, NodeCommon.Apis Apis) : ApiTaskContextBase(Task, Apis), ITaskOutputExecutionContext
    {
        public ITaskOutputInfo Output => Task.Info.Output;
        public ITaskOutputHandler Handler => Task.Info.Output.Type.GetHandler();

        public async Task SetValidationAsync() => await ChangeStateAsync(TaskState.Validation);
    }

    static async Task UploadResult(ITaskOutputExecutionContext context)
    {
        // TODO:: delete
        var task = ((TaskOutputExecutionContext) context).Task;

        CheckFileList(task.InputFileList, "input");
        CheckFileListList(task.OutputFileListList, "output");

        context.LogInfo($"Uploading result to {Newtonsoft.Json.JsonConvert.SerializeObject(context.Output, Newtonsoft.Json.Formatting.None)} ... (wh {OutputSemaphore.CurrentCount})");
        context.LogInfo($"Results: {string.Join(" | ", task.OutputFileListList.Select(l => string.Join(", ", l)))}");
        await context.Handler.UploadResult(task, new ReadOnlyTaskFileList(task.OutputFileListList.SelectMany(l => l)) { OutputJson = task.OutputFileListList.Select(t => t.OutputJson).SingleOrDefault(t => t is not null) }).ConfigureAwait(false);
        context.LogInfo($"Result uploaded");

        await context.SetValidationAsync();
    }


    [Obsolete("Use TaskHandler instead")]
    public static async Task Execute(ReceivedTask task, PluginManager pluginManager)
    {
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.None)}");
        var apis = Apis.Default;

        if (task.State <= TaskState.Input)
        {
            var isqspreview = task.GetFirstAction().Name == TaskAction.GenerateQSPreview;
            var semaphore = isqspreview ? QSPreviewInputSemaphore : NonQSPreviewInputSemaphore;
            var info = isqspreview ? "qspinput" : "input";
            using var _ = await WaitDisposed(semaphore, task, info);

            await DownloadInput(new TaskInputExecutionContext(task, apis));
        }
        else task.LogInfo($"Input seems to be already downloaded");

        if (task.State <= TaskState.Active)
        {
            using var _ = await WaitDisposed(ActiveSemaphore, task);
            await Execute(new TaskExecutionExecutionContext(task, apis, await pluginManager.GetInstalledPluginsAsync()), task.InputFileList);
        }
        else task.LogInfo($"Task execution seems to be already finished");

        if (task.State <= TaskState.Output)
        {
            using var _ = await WaitDisposed(OutputSemaphore, task);
            await UploadResult(new TaskOutputExecutionContext(task, apis));
        }
        else task.LogWarn($"Task result seems to be already uploaded (??????????????)");

        await MaybeNotifyTelegramBotOfTaskCompletion(task);
    }

    /// <summary> Asserts that the provided list isn't empty and all files are present </summary>
    static void CheckFileListList([System.Diagnostics.CodeAnalysis.NotNull] IReadOnlyTaskFileListList? lists, string type)
    {
        if (lists is null)
            throw new NodeTaskFailedException($"Task {type} file list list was null or empty");

        foreach (var list in lists)
            CheckFileList(list, type);
    }

    /// <inheritdoc cref="CheckFileListList"/>
    static void CheckFileList([System.Diagnostics.CodeAnalysis.NotNull] ReadOnlyTaskFileList? files, string type)
    {
        if (files is null)
            throw new NodeTaskFailedException($"Task {type} file list was null or empty");

        foreach (var file in files)
            if (!File.Exists(file.Path))
                throw new NodeTaskFailedException($"Task {type} file {file} does not exists");
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



    record TaskExecutionContextSubtaskOverlay(int Subtask, int MaxSubtasks, ITaskExecutionContext Context) : ITaskExecutionContext
    {
        public IMPlusApi? MPlusApi => Context.MPlusApi;
        public IReadOnlyCollection<Plugin> Plugins => Context.Plugins;

        public void Log(LogLevel level, string text) => Context.Log(level, text);

        public void SetProgress(double progress)
        {
            var subtaskpart = 1d / MaxSubtasks;
            Context.SetProgress((progress * subtaskpart) + (subtaskpart * Subtask));
        }
    }
    record TaskExecutionContext(ReceivedTask Task, IReadOnlyCollection<Plugin> Plugins, IMPlusApi? MPlusApi) : ITaskExecutionContext
    {
        public void Log(LogLevel level, string text) => Task.Log(level, text);

        const int ProgressSendDelaySec = 5;
        DateTime ProgressWriteTime = DateTime.MinValue;
        public void SetProgress(double progress)
        {
            Task.Progress = progress;

            var now = DateTime.Now;
            if (progress >= .98 || ProgressWriteTime < now)
            {
                Apis.Default.SendTaskProgressAsync(Task).Consume();
                ProgressWriteTime = DateTime.Now.AddSeconds(ProgressSendDelaySec);
            }
        }
    }
}
