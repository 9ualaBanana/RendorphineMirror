using Transport.Upload;

namespace Node.Tasks.Handlers;

public class MPlusTaskHandler : ITaskInputHandler, ITaskOutputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.MPlus;
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.MPlus;

    readonly IComponentContext ComponentContext;

    public async ValueTask<ReadOnlyTaskFileList> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var files = new TaskFileList(task.FSInputDirectory());
        var lastex = null as Exception;
        var firstaction = (IFilePluginAction) ComponentContext.ResolveKeyed<IGPluginAction>(GTaskExecutor.GetTaskName(task.Info.Data));

        foreach (var inputformats in firstaction.InputFileFormats.OrderByDescending(fs => fs.Sum(f => (int) f + 1)))
        {
            using var token = new CancellationTokenSource();
            task.LogInfo($"[M+ ITH] (Re)trying to download {string.Join(", ", inputformats)}");

            try
            {
                await Task.WhenAll(inputformats.Select(f => download(f, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, token.Token).Token)));
                return files;
            }
            catch (Exception ex)
            {
                lastex = ex;
                files.Clear();
                token.Cancel();
            }
        }

        task.ThrowFailed("Could not download any input files", lastex?.Message);
        throw null;


        async Task download(FileFormat format, CancellationToken token)
        {
            var downloadLink = await task.ShardGet<string>("gettaskinputdownloadlink", "link", "Getting m+ input download link",
                ("taskid", task.Id), ("format", format.ToString().ToLowerInvariant()), ("original", format == FileFormat.Jpeg ? "1" : "0"));

            using var response = await Api.GlobalClient.GetAsync(downloadLink.ThrowIfError(), HttpCompletionOption.ResponseHeadersRead, token);
            using var file = File.Open(files.New(format).Path, FileMode.Create, FileAccess.Write);

            try { task.LogInfo($"[M+ ITH] {format} file is {response.Content.Headers.ContentLength} bytes"); }
            catch { }

            await response.Content.CopyToAsync(file, cancellationToken);
        }
    }
    public async ValueTask UploadResult(ReceivedTask task, ReadOnlyTaskFileList files, CancellationToken cancellationToken)
    {
        // example: files=["input.g.jpg", "input.t.jpg"] returns "input."
        var commonprefix = files.Paths.Select(Path.GetFileNameWithoutExtension).Cast<string>().Aggregate((seed, z) => string.Join("", seed.TakeWhile((v, i) => z.Length > i && v == z[i])));
        if (commonprefix.EndsWith('.'))
            commonprefix = commonprefix[..^1];

        task.LogInfo($"[MPlus] Uploading {files.Count} files with common prefix '{commonprefix}': {string.Join(", ", files.Paths.Select(Path.GetFileName))}");

        foreach (var file in files.Paths)
        {
            var postfix = Path.GetFileNameWithoutExtension(file);
            if (postfix.StartsWith("output", StringComparison.Ordinal))
                postfix = postfix.Substring("output".Length);
            else postfix = postfix.Substring(commonprefix.Length);

            task.LogInfo($"[MPlus] Uploading {file} with postfix '{postfix}'");
            var iid = await PacketsTransporter.UploadAsync(await MPlusTaskResultUploadSessionData.InitializeAsync(file, postfix: postfix, task, Api.GlobalClient, Settings.SessionId), cancellationToken: cancellationToken);
            task.UploadedFiles.Add(new MPlusUploadedFileInfo(iid, file));
        }
    }

    public ValueTask<bool> CheckCompletion(DbTaskFullState task) => ValueTask.FromResult(task.State == TaskState.Validation && ((MPlusTaskOutputInfo) task.Output).IngesterHost is not null);
    public ValueTask<OperationResult<TaskObject>> GetTaskObject(ITaskInputInfo input) => ((MPlusTaskInputInfo) input).GetFileInfo(Settings.SessionId, Settings.UserId);
}
