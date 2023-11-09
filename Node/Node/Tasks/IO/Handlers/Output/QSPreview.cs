using Node.Tasks.Exec.Output;

namespace Node.Tasks.IO.Handlers.Output;

public static class QSPreview
{
    public const int Version = 8;

    public class UploadHandler : FileTaskUploadHandler<QSPreviewOutputInfo, QSPreviewOutput>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.QSPreview;
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Apis { get; init; }

        protected override async Task UploadResultImpl(QSPreviewOutputInfo info, ITaskInputInfo input, QSPreviewOutput files, CancellationToken token)
        {
            var jpegfooter = files.ImageFooter.ThrowIfNull();
            var jpegqr = files.ImageQr;
            var mov = files.Video;

            var res = await Apis.ShardPost<InitOutputResult>(ApiTask, "initqspreviewoutput", null, "Initializing qs preview result upload", ("taskid", ApiTask.Id));
            if (!res && res.ToString()?.Contains("There is no such user", StringComparison.OrdinalIgnoreCase) == true)
                throw new TaskFailedException(res.ToString());

            var result = res.ThrowIfError();
            using var content = new MultipartFormDataContent()
            {
                { new StringContent(result.UploadId), "uploadid" },
                { new StringContent(Version.ToStringInvariant()), "version" },
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


            var uploadres = await Apis.Api.ApiPost($"{AddSchemeIfNeeded(result.Host, "https")}/content/upload/qspreviews/", "Uploading qs preview", content);
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
    public class CompletionChecker : TaskCompletionChecker<QSPreviewOutputInfo>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.QSPreview;

        public override bool CheckCompletion(QSPreviewOutputInfo info, TaskState state) =>
            state == TaskState.Validation && info.IngesterHost is not null;
    }
}
