namespace Node.Tasks.IO.GHandlers.Output;

public class QSPreviewTaskHandlerInfo : FileTaskOutputHandlerInfo<QSPreviewOutputInfo>
{
    public const string Version = "7";

    public override TaskOutputType Type => TaskOutputType.QSPreview;
    protected override Type UploadHandlerType => typeof(UploadHandler);
    protected override Type CompletionCheckerType => typeof(CompletionChecker);


    class UploadHandler : UploadHandlerBase
    {
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required NodeCommon.Apis Apis { get; init; }

        public override async Task UploadResult(QSPreviewOutputInfo info, ReadOnlyTaskFileList files, CancellationToken token)
        {
            var jpegfooter = files.First(f => f.Format == FileFormat.Jpeg && Path.GetFileNameWithoutExtension(f.Path) == "pj_footer");
            var jpegqr = files.FirstOrDefault(f => f.Format == FileFormat.Jpeg && Path.GetFileNameWithoutExtension(f.Path) == "pj_qr");

            var mov = files.TrySingle(FileFormat.Mov);

            var res = await Apis.ShardPost<InitOutputResult>(ApiTask, "initqspreviewoutput", null, "Initializing qs preview result upload", ("taskid", ApiTask.Id));
            if (!res && res.Message?.Contains("There is no such user", StringComparison.OrdinalIgnoreCase) == true)
                throw new TaskFailedException(res.Message);

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
        static string AddSchemeIfNeeded(string url, string scheme)
        {
            if (!scheme.EndsWith("://", StringComparison.Ordinal)) scheme += "://";

            if (url.StartsWith(scheme, StringComparison.Ordinal)) return url;
            return scheme + url;
        }


        record InitOutputResult(string UploadId, string Host);
    }
    class CompletionChecker : CompletionCheckerBase
    {
        public override bool CheckCompletion(QSPreviewOutputInfo info, TaskState state) =>
            state == TaskState.Validation && info.IngesterHost is not null;
    }
}
