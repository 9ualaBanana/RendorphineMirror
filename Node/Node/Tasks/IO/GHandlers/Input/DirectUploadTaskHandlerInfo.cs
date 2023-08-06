namespace Node.Tasks.IO.GHandlers.Input;

public interface IDirectUploadTaskServerProvider
{
    Task<OperationResult<string>> GetServerAsync(string taskid);
}
public class DirectUploadTaskHandlerInfo : FileTaskInputHandlerInfo<DirectDownloadTaskInputInfo>
{
    public override TaskInputType Type => throw new NotImplementedException();
    protected override Type HandlerType => typeof(Handler);
    protected override Type TaskObjectProviderType => typeof(TaskObjectProvider);
    protected override Type? InputUploaderType => typeof(InputUploader);


    class Handler : FileHandlerBase
    {
        public required IRegisteredTaskApi ApiTask { get; init; }

        public override async Task<ReadOnlyTaskFileList> Download(DirectDownloadTaskInputInfo input, TaskObject obj, CancellationToken token) =>
            await Listeners.DirectUploadListener.WaitForFiles(TaskDirectoryProvider.InputDirectory, ApiTask.Id, obj, token);
    }
    class TaskObjectProvider : TaskObjectProviderBase
    {
        public override Task<OperationResult<TaskObject>> GetTaskObject(DirectDownloadTaskInputInfo input, CancellationToken token) =>
            GetLocalFileTaskObject(input.Path).AsOpResult().AsTask();
    }
    class InputUploader : InputUploaderBase
    {
        public required NodeCommon.Apis Api { get; init; }
        public required IRegisteredTaskApi ApiTask { get; init; }

        public override async Task UploadInputFiles(DirectDownloadTaskInputInfo input)
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
