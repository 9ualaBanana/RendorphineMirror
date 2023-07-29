namespace Node.Tasks.Handlers;

public class QSPreviewTaskHandler : ITaskOutputHandler
{
    public const string Version = "6";

    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.QSPreview;

    public async ValueTask UploadResult(ReceivedTask task, ReadOnlyTaskFileList files, CancellationToken cancellationToken)
    {
        var jpegfooter = files.First(f => f.Format == FileFormat.Jpeg && Path.GetFileNameWithoutExtension(f.Path) == "pj_footer");
        var jpegqr = files.FirstOrDefault(f => f.Format == FileFormat.Jpeg && Path.GetFileNameWithoutExtension(f.Path) == "pj_qr");

        var mov = files.TrySingle(FileFormat.Mov);

        var res = await task.ShardPost<InitOutputResult>("initqspreviewoutput", null, "Initializing qs preview result upload", ("taskid", task.Id));
        if (!res && res.Message?.Contains("There is no such user", StringComparison.OrdinalIgnoreCase) == true)
        {
            res.LogIfError();
            task.ThrowFailed(res.Message);
            return;
        }

        var result = res.ThrowIfError();
        using var content = new MultipartFormDataContent()
        {
            { new StringContent(result.UploadId), "uploadid" },
            { new StringContent(Version), "version" },
            new StreamContent(File.OpenRead(jpegfooter.Path))
            {
                Headers =
                {
                    { "Content-Type", "image/jpeg" },
                    { "Content-Disposition", $"form-data; name=jpeg; filename={Path.GetFileName(jpegfooter.Path)}" },
                },
            },
        };

        if (jpegqr is not null)
        {
            content.Add(new StreamContent(File.OpenRead(jpegqr.Path))
            {
                Headers =
                {
                    { "Content-Type", "image/jpeg" },
                    { "Content-Disposition", $"form-data; name=qr; filename={Path.GetFileName(jpegqr.Path)}" },
                },
            });
        }

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
