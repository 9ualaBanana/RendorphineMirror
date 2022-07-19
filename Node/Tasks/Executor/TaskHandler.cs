using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Executor;

public static class TaskHandler
{
    static TaskInputOutputType GetInputOutputType(JObject json) => Enum.Parse<TaskInputOutputType>(json["type"]!.Value<string>()!);

    static MPlusTaskInputInfo DeserializeInput(TaskInfo info) =>
        GetInputOutputType(info.Input) switch
        {
            TaskInputOutputType.MPlus => info.Input.ToObject<MPlusTaskInputInfo>()!,
            { } type => throw new NotSupportedException($"Task input type {type} is not supported"),
        };
    static ITaskOutputInfo DeserializeOutput(TaskInfo info) =>
        GetInputOutputType(info.Output) switch
        {
            TaskInputOutputType.MPlus => info.Output.ToObject<MPlusTaskOutputInfo>()!,
            { } type => throw new NotSupportedException($"Task output type {type} is not supported"),
        };

    public static async Task HandleAsync(ReceivedTask task, HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        try
        {
            task.LogInfo($"Started");

            var inputobj = DeserializeInput(task.Info);
            var outputobj = DeserializeOutput(task.Info);
            task.LogInfo($"Task info: {JsonConvert.SerializeObject(task, Formatting.Indented)}");

            task.LogInfo($"Downloading file...");
            var input = await inputobj.Download(task, httpClient, cancellationToken).ConfigureAwait(false);
            task.LogInfo($"File downloaded to {input}");
            await task.ChangeStateAsync(TaskState.Active);

            var output = await TaskList.Get(task.Info).Execute(task, input).ConfigureAwait(false);
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
                    var queryString = $"sessionid={Settings.SessionId}&iid={inputobj.Iid}&nodename={Settings.NodeName}";
                    await httpClient.PostAsync($"{Settings.ServerUrl}/tasks/result_preview?{queryString}", null, cancellationToken);
                    task.LogInfo($"Result uploaded");
                }
                catch (Exception ex) { Log.Error("Error sending result to reepo: " + ex); }
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
