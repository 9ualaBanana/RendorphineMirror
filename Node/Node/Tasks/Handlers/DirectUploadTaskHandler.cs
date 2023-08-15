using System.IO.Compression;

namespace Node.Tasks.Handlers;

public class DirectUploadTaskHandler : ITaskInputHandler, ITaskOutputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.DirectUpload;
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.DirectDownload;

    public async ValueTask<ReadOnlyTaskFileList> Download(ReceivedTask task, CancellationToken cancellationToken) =>
        await Listeners.DirectUploadListener.WaitForFiles(task, cancellationToken);

    public async ValueTask UploadResult(ReceivedTask task, ReadOnlyTaskFileList files, CancellationToken cancellationToken = default) =>
        await Listeners.DirectDownloadListener.WaitForUpload(task, cancellationToken);

    public async ValueTask UploadInputFiles(DbTaskFullState task)
    {
        var info = (DirectDownloadTaskInputInfo) task.Input;
        while (true)
        {
            try
            {
                var serverr = await task.GetTaskStateAsync();
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


                task.LogInfo($"Uploading input files...");
                var files = File.Exists(info.Path) ? new[] { info.Path } : Directory.GetFiles(info.Path);
                foreach (var file in files)
                {
                    using var content = new MultipartFormDataContent()
                    {
                        { new StringContent(task.Id), "taskid" },
                        { new StreamContent(File.OpenRead(file)) { Headers = { ContentType = new(MimeTypes.GetMimeType(file)), ContentLength = new FileInfo(file).Length } }, "file", Path.GetFileName(file) },
                        { new StringContent(file == files[^1] ? "1" : "0"), "last" },
                    };

                    var post = await Api.Default.ApiPost($"{server.Host}/rphtaskexec/uploadinput", $"Uploading input files for task {task.Id}", content);
                    post.ThrowIfError();
                    task.LogInfo($"Task files uploaded");
                }

                return;
            }
            catch (Exception ex)
            {
                task.LogErr("Could not upload task input files: " + ex);
                await Task.Delay(10_000);
            }
        }
    }

    public ValueTask<bool> CheckCompletion(DbTaskFullState task) => ValueTask.FromResult(task.State >= TaskState.Output);
    public async ValueTask OnPlacedTaskCompleted(DbTaskFullState task)
    {
        var state = await task.GetTaskStateAsync().ThrowIfError();
        if (state is null) return;

        var server = state.Server;
        if (server is null) task.ThrowFailed("Could not find server in /getmytaskstate request");

        var host = server.Host;
        if (task.IsFromSameNode())
            host = $"127.0.0.1:{Settings.UPnpPort}";

        var info = (DirectUploadTaskOutputInfo) task.Output;
        using var result = await Api.Default.Get($"{server.Host}/rphtaskexec/downloadoutput?taskid={task.Id}");

        var zipfile = task.GetTempFileName("zip");
        using var _ = new FuncDispose(() => File.Delete(zipfile));
        using (var zipstream = File.OpenWrite(zipfile))
            await result.Content.CopyToAsync(zipstream);


        JToken? json = null;
        try
        {
            using var read = new JsonTextReader(new StreamReader(File.OpenRead(zipfile)));
            json = JToken.Load(read);
        }
        catch { }
        if (json is not null && json["ok"]?.Value<bool>() != true)
            throw new Exception(json["errormessage"]!.Value<string>());

        try { ZipFile.ExtractToDirectory(zipfile, task.FSPlacedResultsDirectory()); }
        catch
        {
            var mime = result.Content.Headers.ContentType!.ToString();
            var file = Path.ChangeExtension(Path.GetFileName(zipfile), MimeTypes.GetMimeTypeExtensions(mime).FirstOrDefault());
            File.Move(zipfile, Path.Combine(task.FSPlacedResultsDirectory(), file));
        }
    }

    public ValueTask<OperationResult<TaskObject>> GetTaskObject(ITaskInputInfo input) => ((ILocalTaskInputInfo) input).GetTaskObject();
}
