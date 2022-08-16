using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Executor;

public static class TaskHandler
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    static TaskInputOutputType GetInputOutputType(JObject json)
    {
        var token = json.Property("type", StringComparison.OrdinalIgnoreCase)?.Value!;

        if (token.Type == JTokenType.Integer)
            return Enum.GetValues<TaskInputOutputType>()[token.Value<int>()];
        return Enum.Parse<TaskInputOutputType>(token.Value<string>()!);
    }

    public static ITaskInput DeserializeInput(JObject input) =>
        GetInputOutputType(input) switch
        {
            TaskInputOutputType.MPlus => new MPlusTaskInput(input.ToObject<MPlusTaskInputInfo>()!),
            TaskInputOutputType.User => new UserTaskInput(input.ToObject<UserTaskInputInfo>()!),
            { } type => throw new NotSupportedException($"Task input type {type} is not supported"),
        };
    public static ITaskOutput DeserializeOutput(JObject output) =>
        GetInputOutputType(output) switch
        {
            TaskInputOutputType.MPlus => new MPlusTaskOutput(output.ToObject<MPlusTaskOutputInfo>()!),
            TaskInputOutputType.User => new UserTaskOutput(output.ToObject<UserTaskOutputInfo>()!),
            { } type => throw new NotSupportedException($"Task output type {type} is not supported"),
        };

    public static async Task HandleReceivedTask(ReceivedTask task, CancellationToken token = default)
    {
        try
        {
            NodeSettings.SavedTasks.Bindable.Add(task);
            await HandleAsync(task, token).ConfigureAwait(false);
        }
        catch (Exception ex) { _logger.Error(ex.ToString()); }
        finally
        {
            task.LogInfo($"Removing");
            NodeSettings.SavedTasks.Bindable.Remove(task);
        }
    }
    public static async Task HandleAsync(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        try
        {
            NodeGlobalState.Instance.ExecutingTasks.Add(task);
            using var _ = new FuncDispose(() => NodeGlobalState.Instance.ExecutingTasks.Remove(task));


            task.LogInfo($"Started");

            var inputobj = DeserializeInput(task.Info.Input);
            var outputobj = DeserializeOutput(task.Info.Output);
            task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.Indented)}");

            task.LogInfo($"Downloading file...");
            var input = await inputobj.Download(task, cancellationToken).ConfigureAwait(false);
            task.InputFile = input;
            task.LogInfo($"File downloaded to {input}");
            await task.ChangeStateAsync(TaskState.Active);

            var output = await TaskList.GetAction(task.Info).Execute(task, input).ConfigureAwait(false);
            await task.ChangeStateAsync(TaskState.Output);

            task.LogInfo($"Uploading output file {output} ...");
            await outputobj.Upload(task, output).ConfigureAwait(false);
            task.LogInfo($"File uploaded");
            await task.ChangeStateAsync(TaskState.Finished);

            if (outputobj is MPlusTaskOutput mplusoutput)
            {
                try
                {
                    task.LogInfo($"Uploading result to the reepo...");

                    task.LogInfo($"Getting output iid...");
                    var outiidr = await Apis.GetTaskStateAsync(task.Id)
                        .Next(taskinfo => taskinfo.Output["ingesterhost"]?.Value<string>().AsOpResult() ?? OperationResult.Err("Could not find ingester host"))
                        .Next(ingester => Api.ApiGet<string>($"https://{ingester}/content/vcupload/getiid", "iid", "Getting output iid", ("extid", task.Id)))
                        .ConfigureAwait(false);

                    var outiid = outiidr.ThrowIfError();
                    task.LogInfo($"Got output iid: {outiid}");

                    task.LogInfo($"Uploading...");
                    var queryString = $"sessionid={Settings.SessionId}&iid={outiid}&nodename={Settings.NodeName}";
                    await Api.Client.PostAsync($"{Settings.ServerUrl}/tasks/result_preview?{queryString}", null, cancellationToken);
                    task.LogInfo($"Result uploaded");
                }
                catch (Exception ex) { _logger.Error("Error sending result to reepo: " + ex); }
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            task.LogErr(ex);
            await task.ChangeStateAsync(TaskState.Canceled);
        }
        catch (Exception ex)
        {
            task.LogErr(ex);
            await task.ChangeStateAsync(TaskState.Failed);
        }
    }
}
