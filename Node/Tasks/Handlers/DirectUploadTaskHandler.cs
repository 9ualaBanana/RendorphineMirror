using System.IO.Compression;

namespace Node.Tasks.Handlers;

public class DirectUploadTaskHandler : ITaskInputHandler, ITaskOutputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.DirectUpload;
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.DirectDownload;

    public async ValueTask Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (DirectDownloadTaskInputInfo) task.Input;

        if (task.IsFromSameNode)
        {
            task.SetInput(info.Path);
            info.Downloaded = true;
        }

        var token = new StuckCancellationToken(cancellationToken, TimeSpan.FromHours(1));
        while (!info.Downloaded)
        {
            token.ThrowIfCancellationRequested();
            token.ThrowIfStuck($"Did not receive input files");
            await Task.Delay(2000);
        }
    }
    public ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        // TODO: maybe move instead of copy? but the gallery..
        if (task.IsFromSameNode)
            Extensions.CopyDirectory(task.FSOutputDirectory(), task.FSPlacedResultsDirectory());

        return ValueTask.CompletedTask;
    }

    public async ValueTask InitializePlacedTaskAsync(DbTaskFullState task)
    {
        if (task.State > TaskState.Input) return;
        if (task.IsFromSameNode) return;

        var info = (DirectDownloadTaskInputInfo) task.Input;

        int tries = 0;
        while (true)
        {
            task.LogInfo($"Uploading input files ({tries})...");

            try
            {
                var serverr = await task.GetTaskStateAsync();
                if (!serverr)
                {
                    await Task.Delay(10_000);
                    continue;
                }

                var server = serverr.Value.Server;
                if (server is null)
                {
                    await Task.Delay(10_000);
                    continue;
                }


                var files = File.Exists(info.Path) ? new[] { info.Path } : Directory.GetFiles(info.Path);
                foreach (var file in files)
                {
                    using var content = new MultipartFormDataContent()
                    {
                        { new StringContent(task.Id), "taskid" },
                        { new StreamContent(File.OpenRead(file)) { Headers = { ContentType = new(MimeTypes.GetMimeType(file)) } }, "file", Path.GetFileName(file) },
                        { new StringContent(file == files[^1] ? "1" : "0"), "last" },
                    };

                    var post = await Api.ApiPost($"{server.Host}/rphtaskexec/uploadinput", $"Uploading input files for task {task.Id}", content);
                    post.ThrowIfError();

                    break;
                }

                return;
            }
            catch (Exception ex)
            {
                tries++;
                task.LogErr("Could not upload task input files: " + ex);
                await Task.Delay(10_000);
            }
        }
    }

    public async ValueTask OnPlacedTaskCompleted(DbTaskFullState task)
    {
        if (task.IsFromSameNode) return;

        var server = (await task.GetTaskStateAsync()).ThrowIfError().Server.ThrowIfNull("Could not find server in /getmytaskstate request");

        var info = (DirectUploadTaskOutputInfo) task.Output;
        using var result = await Api.Get($"{server.Host}/rphtaskexec/downloadoutput?taskid={task.Id}");

        var zipfile = task.GetTempFileName("zip");
        using var _ = new FuncDispose(() => File.Delete(zipfile));
        using (var zipstream = File.OpenWrite(zipfile))
            await result.Content.CopyToAsync(zipstream);

        try { ZipFile.ExtractToDirectory(zipfile, task.FSPlacedResultsDirectory()); }
        catch
        {
            task.LogInfo(string.Join(", ", result.Headers));
            task.LogInfo(string.Join(", ", result.Content.Headers));
            var mime = result.Content.Headers.ContentType!.ToString();
            var file = Path.ChangeExtension(Path.GetFileName(zipfile), MimeTypes.GetMimeTypeExtensions(mime).FirstOrDefault());
            File.Move(zipfile, Path.Combine(task.FSPlacedResultsDirectory(), file));
        }
    }
}
