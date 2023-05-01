using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Node.Tasks.Exec;

public record IOTaskExecutionLayerData(IReadOnlyTaskFileListList InputFiles, TaskFileListList OutputFiles);

public record IOTaskExecutionData(IReadOnlyTaskFileList InputFiles, TaskFileListList OutputFiles);
public record IOTaskCheckData(IReadOnlyTaskFileList InputFiles, IReadOnlyTaskFileList OutputFiles);
public interface IInputOutputPluginAction
{
    Task JustExecute(ReceivedTask task, IOTaskExecutionData files, JObject data);
}
public abstract class InputOutputPluginAction<T> : PluginAction<T>, IInputOutputPluginAction
{
    protected abstract OperationResult ValidateOutputFiles(IOTaskCheckData files, T data);

    protected void ValidateInputFilesThrow(ReceivedTask task, IReadOnlyTaskFileList files) =>
        ValidateInputFiles(files).ThrowIfError($"Task {task.Id} input file validation failed: {{0}}");
    OperationResult ValidateInputFiles(IReadOnlyTaskFileList files) => TaskRequirement.EnsureFormats(files, "input", InputFileFormats);


    static readonly SemaphoreSlim InputSemaphore = new SemaphoreSlim(5);
    static readonly SemaphoreSlim TaskWaitHandle = new SemaphoreSlim(1);
    static readonly SemaphoreSlim OutputSemaphore = new SemaphoreSlim(5);

    static async Task<FuncDispose> WaitDisposed(string info, ReceivedTask task, SemaphoreSlim semaphore)
    {
        task.LogInfo($"Waiting for the {info} handle: (wh {semaphore.CurrentCount})");

        await semaphore.WaitAsync();
        return FuncDispose.Create(semaphore.Release);
    }

    protected sealed override async Task Execute(ReceivedTask task, T data)
    {
        task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.None)}");

        if (task.State <= TaskState.Input)
        {
            using var _ = await WaitDisposed("input", task, InputSemaphore);

            task.LogInfo($"Downloading input... (wh {InputSemaphore.CurrentCount})");
            var inputfiles = await task.GetInputHandler().Download(task).ConfigureAwait(false);
            task.LogInfo($"Input downloaded from {Newtonsoft.Json.JsonConvert.SerializeObject(task.Info.Input, Newtonsoft.Json.Formatting.None)}");

            foreach (var jpeg in inputfiles.Where(f => f.Format == FileFormat.Jpeg).ToArray())
            {
                using var img = Image.Load<Rgba32>(jpeg.Path);
                if (img.Metadata.ExifProfile?.GetValue(ExifTag.Orientation) is not null)
                {
                    img.Mutate(ctx => ctx.AutoOrient());
                    await img.SaveAsJpegAsync(jpeg.Path, new JpegEncoder() { Quality = 100 });
                }
            }

            task.LogInfo($"Validating downloaded files...");
            ValidateInputFilesThrow(task, inputfiles);

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
            await JustExecute(task, new IOTaskExecutionData(task.InputFileList, outputs), data);

            int index = 0;
            foreach (var next in task.Info.Next ?? ImmutableArray<Newtonsoft.Json.Linq.JObject>.Empty)
            {
                index++;

                var action = TaskList.GetAction(TaskInfo.GetTaskType(next));
                if (action is not IInputOutputPluginAction ioaction)
                {
                    task.ThrowFailed($"Invalid next task action type {action.Name} {action.GetType().Name}");
                    throw null;
                }

                var prevoutput = outputs;
                outputs = new TaskFileListList(task.FSOutputDirectory(index.ToString()));

                foreach (var input in prevoutput)
                {
                    outputs.InputFiles = input;
                    await ioaction.JustExecute(task, new IOTaskExecutionData(input, outputs), next);
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
            await task.GetOutputHandler().UploadResult(task, new TaskFileList("/does/not/exists", task.OutputFileListList.SelectMany(l => l))).ConfigureAwait(false);
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
        void checkFileList([System.Diagnostics.CodeAnalysis.NotNull] IReadOnlyTaskFileList? files, string type)
        {
            if (files is (null or { Count: 0 }))
                task.ThrowFailed($"Task {type} file list was null or empty");

            foreach (var file in files)
                if (!File.Exists(file.Path))
                    task.ThrowFailed($"Task {type} file {file} does not exists");
        }
    }

    Task IInputOutputPluginAction.JustExecute(ReceivedTask task, IOTaskExecutionData files, JObject data) => JustExecute(task, files, data.ToObject<T>().ThrowIfNull());
    async Task JustExecute(ReceivedTask task, IOTaskExecutionData files, T data)
    {
        task.LogInfo($"Executing {Name} {JsonConvert.SerializeObject(data)}");

        task.LogInfo($"Validating input files");
        ValidateInputFilesThrow(task, files.InputFiles);

        task.LogInfo($"Executing");
        await ExecuteImpl(task, files, data).ConfigureAwait(false);

        task.LogInfo($"Task executed, validating result");
        foreach (var outputlist in files.OutputFiles)
            ValidateOutputFiles(new(files.InputFiles, outputlist), data);
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

    protected abstract Task ExecuteImpl(ReceivedTask task, IOTaskExecutionData files, T data);
}
