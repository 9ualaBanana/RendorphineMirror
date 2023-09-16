using System.IO.Compression;

namespace Node.Tasks.IO.Handlers.Output;

public static class DirectDownload
{
    public class UploadHandler : FileTaskUploadHandler<DirectDownloadTaskOutputInfo>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.DirectDownload;
        public required IRegisteredTaskApi ApiTask { get; init; }

        protected override async Task UploadResultImpl(DirectDownloadTaskOutputInfo info, ReadOnlyTaskFileList result, CancellationToken token) =>
            await Listeners.DirectDownloadListener.WaitForUpload(ApiTask.Id, token);
    }
    public class CompletionChecker : TaskCompletionChecker<DirectDownloadTaskOutputInfo>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.DirectDownload;

        public override bool CheckCompletion(DirectDownloadTaskOutputInfo info, TaskState state) =>
            state >= TaskState.Output;
    }
    public class CompletionHandler : TaskCompletionHandler<DirectDownloadTaskOutputInfo>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.DirectDownload;
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Api { get; init; }
        public required ITaskOutputDirectoryProvider DirectoryProvider { get; init; }
        public required NodeSettingsInstance NodeSettings { get; init; }
        public required DataDirs Dirs { get; init; }

        public override async Task OnPlacedTaskCompleted(DirectDownloadTaskOutputInfo info)
        {
            var state = await Api.GetTaskStateAsync(ApiTask).ThrowIfError();
            if (state is null) return;

            var server = state.Server;
            if (server is null)
                throw new TaskFailedException("Could not find server in /getmytaskstate request");

            var host = server.Host;
            if (ApiTask.IsFromSameNode(NodeSettings))
                host = $"127.0.0.1:{Settings.UPnpPort}";

            using var result = await Api.Api.Get($"{host}/rphtaskexec/downloadoutput?taskid={ApiTask.Id}");

            using var _ = Directories.DisposeDelete(Dirs.TempFile($"task_{ApiTask.Id}/zip"), out var zipfile);
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
