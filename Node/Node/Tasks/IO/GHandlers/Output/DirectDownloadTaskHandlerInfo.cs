using System.IO.Compression;

namespace Node.Tasks.IO.GHandlers.Output;

public class DirectDownloadTaskHandlerInfo : FileTaskOutputHandlerInfo<DirectUploadTaskOutputInfo>
{
    public override TaskOutputType Type => TaskOutputType.DirectDownload;

    protected override Type UploadHandlerType => typeof(UploadHandler);
    protected override Type CompletionCheckerType => typeof(CompletionChecker);
    protected override Type CompletedHandlerType => typeof(CompletedHandler);


    class UploadHandler : UploadHandlerBase
    {
        public required IRegisteredTaskApi ApiTask { get; init; }

        public override async Task UploadResult(DirectUploadTaskOutputInfo info, ReadOnlyTaskFileList result, CancellationToken token) =>
            await Listeners.DirectDownloadListener.WaitForUpload(ApiTask.Id, token);
    }
    class CompletionChecker : CompletionCheckerBase
    {
        public override bool CheckCompletion(DirectUploadTaskOutputInfo info, TaskState state) => state >= TaskState.Output;
    }
    class CompletedHandler : CompletedHandlerBase
    {
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required NodeCommon.Apis Api { get; init; }
        public required ITaskOutputDirectoryProvider DirectoryProvider { get; init; }

        public override async Task OnPlacedTaskCompleted(DirectUploadTaskOutputInfo info)
        {
            var state = await Api.GetTaskStateAsync(ApiTask).ThrowIfError();
            if (state is null) return;

            var server = state.Server;
            if (server is null)
                throw new TaskFailedException("Could not find server in /getmytaskstate request");

            var host = server.Host;
            if (ApiTask.IsFromSameNode())
                host = $"127.0.0.1:{Settings.UPnpPort}";

            using var result = await Api.Api.Get($"{host}/rphtaskexec/downloadoutput?taskid={ApiTask.Id}");

            using var _ = Directories.TempFile(out var zipfile, "task" + ApiTask.Id, "zip");
            using (var zipstream = File.OpenWrite(zipfile))
                await result.Content.CopyToAsync(zipstream);


            JToken? json = null;
            try
            {
                using var read = new JsonTextReader(new StreamReader(File.OpenRead(zipfile)));
                json = JToken.Load(read);
            }
            catch { }
            if (json is not null)
                throw new Exception(json["errormessage"]?.Value<string>() ?? "Unknown file data received");

            try { ZipFile.ExtractToDirectory(zipfile, DirectoryProvider.OutputDirectory); }
            catch
            {
                var mime = result.Content.Headers.ContentType!.ToString();
                var file = Path.ChangeExtension(Path.GetFileName(zipfile), MimeTypes.GetMimeTypeExtensions(mime).FirstOrDefault());
                File.Move(zipfile, Path.Combine(DirectoryProvider.OutputDirectory, file));
            }
        }
    }
}
