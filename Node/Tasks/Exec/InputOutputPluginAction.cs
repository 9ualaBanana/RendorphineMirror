using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Exec;

public interface IInputOutputPluginAction
{
    Task JustExecute(ReceivedTask task, JObject data);
}
public abstract class InputOutputPluginAction<T> : PluginAction<T>, IInputOutputPluginAction
{
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
            await task.GetInputHandler().Download(task).ConfigureAwait(false);
            task.LogInfo($"Input downloaded from {Newtonsoft.Json.JsonConvert.SerializeObject(task.Info.Input, Newtonsoft.Json.Formatting.None)}");
            task.LogInfo($"Validating downloaded files...");
            ValidateInputFilesThrow(task);

            await task.ChangeStateAsync(TaskState.Active);
        }
        else task.LogInfo($"Input seems to be already downloaded");

        if (task.State <= TaskState.Active)
        {
            using var _ = await WaitDisposed("active", task, TaskWaitHandle);
            await JustExecute(task, data);

            foreach (var next in task.Info.Next ?? ImmutableArray<Newtonsoft.Json.Linq.JObject>.Empty)
            {
                var action = TaskList.GetAction(TaskInfo.GetTaskType(next));
                if (action is not IInputOutputPluginAction ioaction)
                {
                    task.ThrowFailed($"Invalid next task action type {action.Name} {action.GetType().Name}");
                    throw null;
                }

                await ioaction.JustExecute(task, next);
            }

            await task.ChangeStateAsync(TaskState.Output);
        }
        else task.LogInfo($"Task execution seems to be already finished");

        if (task.State <= TaskState.Output)
        {
            using var _ = await WaitDisposed("output", task, OutputSemaphore);

            task.LogInfo($"Validating output files...");
            ValidateOutputFiles(task, data);

            task.LogInfo($"Uploading result to {Newtonsoft.Json.JsonConvert.SerializeObject(task.Info.Output, Newtonsoft.Json.Formatting.None)} ... (wh {OutputSemaphore.CurrentCount})");
            await task.GetOutputHandler().UploadResult(task).ConfigureAwait(false);
            task.LogInfo($"Result uploaded");

            await task.ChangeStateAsync(TaskState.Validation);
        }
        else task.LogWarn($"Task result seems to be already uploaded (??????????????)");

        await MaybeNotifyTelegramBotOfTaskCompletion(task);
    }

    Task IInputOutputPluginAction.JustExecute(ReceivedTask task, JObject data) => JustExecute(task, data.ToObject<T>().ThrowIfNull());
    async Task JustExecute(ReceivedTask task, T data)
    {
        task.LogInfo($"Executing {Name} {JsonConvert.SerializeObject(data)}");

        task.LogInfo($"Validating input files");
        ValidateInputFilesThrow(task);

        task.LogInfo($"Executing");
        await ExecuteImpl(task, data).ConfigureAwait(false);

        task.LogInfo($"Task executed, validating result");
        ValidateOutputFiles(task, data);
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

    protected abstract Task ExecuteImpl(ReceivedTask task, T data);
}
