using System.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Node.Tasks.Exec;

public static class TaskExecutor
{
    static readonly SemaphoreSlim InputSemaphore = new SemaphoreSlim(5);
    static readonly SemaphoreSlim TaskWaitHandle = new SemaphoreSlim(1);
    static readonly SemaphoreSlim OutputSemaphore = new SemaphoreSlim(5);

    static async Task<FuncDispose> WaitDisposed(string info, ILoggable task, SemaphoreSlim semaphore)
    {
        task.LogInfo($"Waiting for the {info} handle: (wh {semaphore.CurrentCount})");

        await semaphore.WaitAsync();
        return FuncDispose.Create(semaphore.Release);
    }


    [Obsolete("Use TaskHandler instead")]
    public static async Task Execute(ReceivedTask task)
    {
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.None)}");
        var context = new TaskExecutionContext(task);

        if (task.State <= TaskState.Input)
        {
            using var _ = await WaitDisposed("input", task, InputSemaphore);

            task.LogInfo($"Downloading input... (wh {InputSemaphore.CurrentCount})");
            var inputfiles = await task.GetInputHandler().Download(task).ConfigureAwait(false);
            task.LogInfo($"Input downloaded from {Newtonsoft.Json.JsonConvert.SerializeObject(task.Info.Input, Newtonsoft.Json.Formatting.None)}");

            foreach (var jpeg in inputfiles.Where(f => f.Format == FileFormat.Jpeg).ToArray())
            {
                using var img = Image.Load<Rgba32>(jpeg.Path);
                if (img.Metadata.ExifProfile?.TryGetValue(ExifTag.Orientation, out var exif) == true && exif is not null)
                {
                    img.Mutate(ctx => ctx.AutoOrient());
                    await img.SaveAsJpegAsync(jpeg.Path, new JpegEncoder() { Quality = 100 });
                }
            }

            task.LogInfo($"Validating downloaded files...");
            task.GetFirstAction().ValidateInputFilesThrow(context, inputfiles);

            await task.ChangeStateAsync(TaskState.Active);
            task.InputFileList = inputfiles;
            NodeSettings.QueuedTasks.Save(task);
        }
        else task.LogInfo($"Input seems to be already downloaded");

        if (task.State <= TaskState.Active)
        {
            checkFileList(task.InputFileList, "input");
            using var _ = await WaitDisposed("active", task, TaskWaitHandle);

            var outputs = new TaskFileListList(task.FSOutputDirectory());
            await task.GetFirstAction().Execute(
                new TaskExecutionContextSubtaskOverlay(0, (task.Info.Next ?? ImmutableArray<JObject>.Empty).Length + 1, context),
                new TaskFiles(task.InputFileList, outputs),
                task.Info.Data
            );

            int index = 0;
            foreach (var next in task.Info.Next ?? ImmutableArray<JObject>.Empty)
            {
                index++;

                var action = TaskList.GetAction(TaskInfo.GetTaskType(next));

                var prevoutput = outputs;
                outputs = new TaskFileListList(task.FSOutputDirectory(index.ToString()));

                foreach (var input in prevoutput)
                {
                    outputs.InputFiles = input;
                    await action.Execute(
                        new TaskExecutionContextSubtaskOverlay(index, task.Info.Next!.Value.Length + 1, context),
                        new TaskFiles(input, outputs),
                        next
                    );
                }
            }

            await task.ChangeStateAsync(TaskState.Output);
            task.OutputFileListList = outputs;
            NodeSettings.QueuedTasks.Save(task);
        }
        else task.LogInfo($"Task execution seems to be already finished");

        if (task.State <= TaskState.Output)
        {
            checkFileList(task.InputFileList, "input");
            checkFileListList(task.OutputFileListList, "output");
            using var _ = await WaitDisposed("output", task, OutputSemaphore);

            task.LogInfo($"Uploading result to {Newtonsoft.Json.JsonConvert.SerializeObject(task.Info.Output, Newtonsoft.Json.Formatting.None)} ... (wh {OutputSemaphore.CurrentCount})");
            await task.GetOutputHandler().UploadResult(task, new ReadOnlyTaskFileList(task.OutputFileListList.SelectMany(l => l))).ConfigureAwait(false);
            task.LogInfo($"Result uploaded");

            await task.ChangeStateAsync(TaskState.Validation);
        }
        else task.LogWarn($"Task result seems to be already uploaded (??????????????)");

        await MaybeNotifyTelegramBotOfTaskCompletion(task);


        void checkFileListList([System.Diagnostics.CodeAnalysis.NotNull] IReadOnlyTaskFileListList? lists, string type)
        {
            if (lists is null)
                task.ThrowFailed($"Task {type} file list list was null or empty");

            foreach (var list in lists)
                checkFileList(list, type);
        }
        void checkFileList([System.Diagnostics.CodeAnalysis.NotNull] ReadOnlyTaskFileList? files, string type)
        {
            if (files is null or { Count: 0 })
                task.ThrowFailed($"Task {type} file list was null or empty");

            foreach (var file in files)
                if (!File.Exists(file.Path))
                    task.ThrowFailed($"Task {type} file {file} does not exists");
        }
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

        try { await Api.Client.PostAsync($"{endpoint}?{queryString}", content: null, cancellationToken); }
        catch (Exception ex) { task.LogErr("Error sending result to Telegram bot: " + ex); }
    }


    record TaskExecutionContextSubtaskOverlay(int Subtask, int MaxSubtasks, ITaskExecutionContext Context) : ITaskExecutionContext
    {
        public IReadOnlyCollection<Plugin> Plugins => Context.Plugins;

        public void Log(LogLevel level, string text) => Context.Log(level, text);

        public void SetProgress(double progress)
        {
            var subtaskpart = 1d / MaxSubtasks;
            Context.SetProgress((progress * subtaskpart) + (subtaskpart * Subtask));
        }
    }
    record TaskExecutionContext(ReceivedTask Task) : ITaskExecutionContext
    {
        public IReadOnlyCollection<Plugin> Plugins => PluginsManager.GetInstalledPluginsCache().ThrowIfNull("Could not launch the task without plugin list being cached");

        public void Log(LogLevel level, string text) => Task.Log(level, text);

        const int ProgressSendDelaySec = 5;
        DateTime ProgressWriteTime = DateTime.MinValue;
        public void SetProgress(double progress)
        {
            var now = DateTime.Now;
            if (progress >= .98 || ProgressWriteTime < now)
            {
                Apis.Default.SendTaskProgressAsync(Task).Consume();
                ProgressWriteTime = DateTime.Now.AddSeconds(ProgressSendDelaySec);
            }

            Task.Progress = progress;
        }
    }
}
