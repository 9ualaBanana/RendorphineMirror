namespace Node.Tasks.Handlers;

public class TitleKeywordsTaskHandler : ITaskInputHandler, ITaskOutputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.TitleKeywords;
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.TitleKeywords;

    public ValueTask<ReadOnlyTaskFileList> Download(ReceivedTask task, CancellationToken cancellationToken = default) =>
        new ReadOnlyTaskFileList(Enumerable.Empty<FileWithFormat>())
        {
            OutputJson = JToken.FromObject((TitleKeywordsInputInfo) task.Input)
        }.AsVTask();

    public ValueTask<OperationResult<TaskObject>> GetTaskObject(ITaskInputInfo input) => new TaskObject("tk.tk", 0).AsTaskResult();

    public async ValueTask UploadResult(ReceivedTask task, ReadOnlyTaskFileList files, CancellationToken cancellationToken = default)
    {
        // TODO:: TEMPORARY AND WILL BE REFACTORED
        var title = (files.OutputJson?["Title"]?.Value<string>()).ThrowIfNull();
        var keywords = (files.OutputJson?["Keywords"]?.ToObject<string[]>()).ThrowIfNull();

        await Api.Default.ApiPost($"{Api.ServerUri}/rphtasklauncher/settaskoutputtitlekeywords", "setting task output title&keywords",
            Apis.Default.AddSessionId(("taskid", task.Id), ("title", title), ("keywords", JsonConvert.SerializeObject(keywords)))
        ).ThrowIfError();
    }
}
