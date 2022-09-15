namespace Node.Tasks.Handlers;

public class QSPreviewTaskHandler : ITaskOutputHandler
{
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.QSPreview;

    public ImmutableArray<string>? AllowedTaskTypes { get; } = ImmutableArray.Create("QSPreview"); // TODO: update task action name when created
    public ImmutableArray<string>? DisallowedTaskTypes => null;

    public async ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken)
    {
        foreach (var file in Directory.GetFiles(task.FSOutputDirectory()))
            await Upload(file);


        async Task Upload(string file)
        {
            var res = await Api.ApiPost<InitOutputResult>($"{Api.TaskManagerEndpoint}/initqspreviewoutput", null, "Initializing qs preview result upload", ("sessionid", Settings.SessionId), ("taskid", task.Id));
            var result = res.ThrowIfError();


            using var jpegcontent = new StreamContent(File.OpenRead(file))
            {
                Headers =
                {
                    { "Content-Type", "image/jpeg" },
                    { "Content-Disposition", $"form-data; name=jpeg; filename={Path.GetFileName(file)}" },
                },
            };

            using var content = new MultipartFormDataContent()
            {
                { new StringContent(result.UploadId), "uploadid" },
                jpegcontent,
                // { new StringContent(), "mp4" },
            };

            var uploadres = await Api.ApiPost($"{HttpHelper.AddSchemeIfNeeded(result.Host, "https")}/content/upload/qspreviews/", "Uploading qs preview", content);
            uploadres.ThrowIfError();
        }
    }

    public async ValueTask<bool> CheckCompletion(DbTaskFullState task)
    {
        var state = (await task.GetTaskStateAsync()).ThrowIfError();
        task.State = state.State;

        // not null if upload is completed
        return state.State == TaskState.Output && state.Output["ingesterhost"] is not null;
    }


    record InitOutputResult(string UploadId, string Host);
}
