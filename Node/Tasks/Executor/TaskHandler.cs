﻿using Newtonsoft.Json;
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

    public static ITaskInputInfo DeserializeInput(JObject input) =>
        GetInputOutputType(input) switch
        {
            TaskInputOutputType.MPlus => input.ToObject<MPlusTaskInputInfo>()!,
            TaskInputOutputType.User => input.ToObject<UserTaskInputInfo>()!,
            { } type => throw new NotSupportedException($"Task input type {type} is not supported"),
        };
    public static ITaskOutputInfo DeserializeOutput(JObject output) =>
        GetInputOutputType(output) switch
        {
            TaskInputOutputType.MPlus => output.ToObject<MPlusTaskOutputInfo>()!,
            TaskInputOutputType.User => output.ToObject<UserTaskOutputInfo>()!,
            { } type => throw new NotSupportedException($"Task output type {type} is not supported"),
        };

    public static async Task HandleReceivedTask(ReceivedTask task, HttpClient? httpClient = null, CancellationToken token = default)
    {
        try
        {
            NodeSettings.SavedTasks.Add(task);
            await HandleAsync(task, httpClient, token).ConfigureAwait(false);
        }
        catch (Exception ex) { _logger.Error(ex.ToString()); }
        finally
        {
            task.LogInfo($"Removing");
            NodeSettings.SavedTasks.Remove(task);
        }
    }
    public static async Task HandleAsync(ReceivedTask task, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        try
        {
            httpClient ??= new();

            NodeGlobalState.Instance.ExecutingTasks.Add(task);
            using var _ = new FuncDispose(() => NodeGlobalState.Instance.ExecutingTasks.Remove(task));


            task.LogInfo($"Started");

            var inputobj = DeserializeInput(task.Info.Input);
            var outputobj = DeserializeOutput(task.Info.Output);
            task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.Indented)}");

            task.LogInfo($"Downloading file...");
            var input = await inputobj.Download(task, cancellationToken).ConfigureAwait(false);
            task.LogInfo($"File downloaded to {input}");
            await task.ChangeStateAsync(TaskState.Active);

            var output = await TaskList.GetAction(task.Info).Execute(task, input).ConfigureAwait(false);
            await task.ChangeStateAsync(TaskState.Output);

            task.LogInfo($"Uploading output file {output} ...");
            await outputobj.Upload(task, output).ConfigureAwait(false);
            task.LogInfo($"File uploaded");
            await task.ChangeStateAsync(TaskState.Finished);

            if (outputobj is MPlusTaskOutputInfo mplusoutput)
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
                    await httpClient.PostAsync($"{Settings.ServerUrl}/tasks/result_preview?{queryString}", null, cancellationToken);
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
