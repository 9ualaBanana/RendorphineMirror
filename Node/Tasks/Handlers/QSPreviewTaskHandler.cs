namespace Node.Tasks.Handlers;

public class QSPreviewTaskHandler : ITaskOutputHandler
{
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.QSPreview;

    public async ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken)
    {
        var jpeg = task.FSOutputFile(FileFormat.Jpeg);
        var mov = task.TryFSOutputFile(FileFormat.Mov);

        var res = await Api.ApiPost<InitOutputResult>($"{Api.TaskManagerEndpoint}/initqspreviewoutput", null, "Initializing qs preview result upload", ("sessionid", Settings.SessionId), ("taskid", task.Id));
        if (!res && res.Message?.Contains("There is no such user", StringComparison.OrdinalIgnoreCase) == true)
        {
            res.LogIfError();
            task.ThrowFailed(res.Message);
            return;
        }

        var result = res.ThrowIfError();
        using var content = new MultipartFormDataContent() { { new StringContent(result.UploadId), "uploadid" }, };

        content.Add(new StreamContent(File.OpenRead(jpeg))
        {
            Headers =
            {
                { "Content-Type", "image/jpeg" },
                { "Content-Disposition", $"form-data; name=jpeg; filename={Path.GetFileName(jpeg)}" },
            },
        });

        if (mov is not null)
        {
            content.Add(new StreamContent(File.OpenRead(mov))
            {
                Headers =
                {
                    { "Content-Type", "video/mp4" },
                    { "Content-Disposition", $"form-data; name=mp4; filename={Path.GetFileName(mov)}" },
                },
            });
        }


        var uploadres = await Api.ApiPost($"{HttpHelper.AddSchemeIfNeeded(result.Host, "https")}/content/upload/qspreviews/", "Uploading qs preview", content);
        uploadres.ThrowIfError();
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
