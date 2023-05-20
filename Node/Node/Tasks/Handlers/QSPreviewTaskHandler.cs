namespace Node.Tasks.Handlers;

public class QSPreviewTaskHandler : ITaskOutputHandler
{
    public const string Version = "4";

    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.QSPreview;

    public async ValueTask UploadResult(ReceivedTask task, ReadOnlyTaskFileList files, CancellationToken cancellationToken)
    {
        var jpeg = files.Single(FileFormat.Jpeg);
        var mov = files.TrySingle(FileFormat.Mov);

        var res = await task.ShardPost<InitOutputResult>("initqspreviewoutput", null, "Initializing qs preview result upload", ("taskid", task.Id));
        if (!res && res.Message?.Contains("There is no such user", StringComparison.OrdinalIgnoreCase) == true)
        {
            res.LogIfError();
            task.ThrowFailed(res.Message);
            return;
        }

        var result = res.ThrowIfError();
        using var content = new MultipartFormDataContent() {
            { new StringContent(result.UploadId), "uploadid" },
            { new StringContent(Version), "version" },
        };

        content.Add(new StreamContent(File.OpenRead(jpeg.Path))
        {
            Headers =
            {
                { "Content-Type", "image/jpeg" },
                { "Content-Disposition", $"form-data; name=jpeg; filename={Path.GetFileName(jpeg.Path)}" },
            },
        });

        if (mov is not null)
        {
            content.Add(new StreamContent(File.OpenRead(mov.Path))
            {
                Headers =
                {
                    { "Content-Type", "video/mp4" },
                    { "Content-Disposition", $"form-data; name=mp4; filename={Path.GetFileName(mov.Path)}" },
                },
            });
        }


        var uploadres = await Api.Default.ApiPost($"{AddSchemeIfNeeded(result.Host, "https")}/content/upload/qspreviews/", "Uploading qs preview", content);
        uploadres.ThrowIfError();
    }

    public ValueTask<bool> CheckCompletion(DbTaskFullState task) => ValueTask.FromResult(task.State == TaskState.Validation && ((QSPreviewOutputInfo) task.Output).IngesterHost is not null);

    static string AddSchemeIfNeeded(string url, string scheme)
    {
        if (!scheme.EndsWith("://", StringComparison.Ordinal)) scheme += "://";

        if (url.StartsWith(scheme, StringComparison.Ordinal)) return url;
        return scheme + url;
    }


    record InitOutputResult(string UploadId, string Host);
}
