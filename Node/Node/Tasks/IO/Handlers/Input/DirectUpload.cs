namespace Node.Tasks.IO.Handlers.Input;

public interface IDirectUploadTaskServerProvider
{
    Task<OperationResult<string>> GetServerAsync(string taskid);
}
public static class DirectUpload
{
    public class InputDownloader : FileTaskInputDownloader<DirectUploadTaskInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.DirectUpload;

        public required IRegisteredTaskApi ApiTask { get; init; }
        public required IDirectUploadTaskServerProvider DirectUploadTaskServerProvider { get; init; }

        protected override async Task<ReadOnlyTaskFileList> DownloadImpl(DirectUploadTaskInputInfo input, TaskObject obj, CancellationToken token) =>
            await Listeners.DirectUploadListener.WaitForFiles(TaskDirectoryProvider.InputDirectory, ApiTask.Id, obj, token);
    }
    public class TaskObjectProvider : LocalFileTaskObjectProvider<DirectUploadTaskInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.DirectUpload;
    }
    public class InputUploader : FileTaskInputUploader<DirectUploadTaskInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.DirectUpload;

        public required NodeCommon.Apis Api { get; init; }
        public required IRegisteredTaskApi ApiTask { get; init; }

        public override async Task Upload(DirectUploadTaskInputInfo input)
        {
            while (true)
            {
                try
                {
                    var serverr = await Api.GetTaskStateAsync(ApiTask);
                    if (!serverr)
                    {
                        await Task.Delay(10_000);
                        continue;
                    }
                    if (serverr.Value is null) return;

                    var server = serverr.Value.Server;
                    if (server is null)
                    {
                        await Task.Delay(10_000);
                        continue;
                    }


                    Logger.LogInformation($"Uploading input files...");
                    var files = File.Exists(input.Path) ? new[] { input.Path } : Directory.GetFiles(input.Path);
                    foreach (var file in files)
                    {
                        using var content = new MultipartFormDataContent()
                        {
                            { new StringContent(ApiTask.Id), "taskid" },
                            { new StreamContent(File.OpenRead(file)) { Headers = { ContentType = new(MimeTypes.GetMimeType(file)), ContentLength = new FileInfo(file).Length } }, "file", Path.GetFileName(file) },
                            { new StringContent(file == files[^1] ? "1" : "0"), "last" },
                        };

                        var post = await Api.Api.ApiPost($"{server.Host}/rphtaskexec/uploadinput", $"Uploading input files for task {ApiTask.Id}", content);
                        post.ThrowIfError();
                        Logger.LogInformation($"Task files uploaded");
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Logger.LogError("Could not upload task input files: " + ex);
                    await Task.Delay(10_000);
                }
            }
        }
    }
}