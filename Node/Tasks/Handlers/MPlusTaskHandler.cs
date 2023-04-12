using Transport.Upload;

namespace Node.Tasks.Handlers;

public class MPlusTaskHandler : ITaskInputHandler, ITaskOutputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.MPlus;
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.MPlus;

    public async ValueTask<TaskFileList> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var files = new TaskFileList(task.FSInputDirectory());
        try { await Task.WhenAll(task.GetFirstAction().InputFileFormats.SelectMany(f => f).Distinct().Select(download)); }
        catch { }

        return files;


        async Task download(FileFormat format)
        {
            var downloadLink = await task.ShardGet<string>("gettaskinputdownloadlink", "link", "Getting m+ input download link",
                ("taskid", task.Id), ("format", format.ToString().ToLowerInvariant()), ("original", format == FileFormat.Jpeg ? "1" : "0"));

            using var inputStream = await Api.Default.Download(downloadLink.ThrowIfError());
            using var file = File.Open(files.FSNewFile(format), FileMode.Create, FileAccess.Write);
            await inputStream.CopyToAsync(file, cancellationToken);
        }
    }
    public async ValueTask UploadResult(ReceivedTask task, IReadOnlyTaskFileList files, CancellationToken cancellationToken)
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
            var iid = await PacketsTransporter.UploadAsync(await MPlusTaskResultUploadSessionData.InitializeAsync(file, postfix: postfix, task, Api.Client, Settings.SessionId), cancellationToken: cancellationToken);
            task.UploadedFiles.Add(new MPlusUploadedFileInfo(iid, file));
        }
    }

    public ValueTask<bool> CheckCompletion(DbTaskFullState task) => ValueTask.FromResult(task.State == TaskState.Validation && ((MPlusTaskOutputInfo) task.Output).IngesterHost is not null);
    public ValueTask<OperationResult<TaskObject>> GetTaskObject(ITaskInputInfo input) => ((MPlusTaskInputInfo) input).GetFileInfo(Settings.SessionId, Settings.UserId);
}
