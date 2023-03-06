using Transport.Upload;

namespace Node.Tasks.Handlers;

public class MPlusTaskHandler : ITaskInputHandler, ITaskOutputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.MPlus;
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.MPlus;

    public async ValueTask Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        foreach (var requirement in task.GetAction().InputRequirements)
        {
            try { await download(requirement.Format); }
            catch { if (requirement.Required) throw; }
        }


        async Task download(FileFormat format)
        {
            var downloadLink = await task.ShardGet<string>("gettaskinputdownloadlink", "link", "Getting m+ input download link",
                ("taskid", task.Id), ("format", format.ToString().ToLowerInvariant()), ("original", format == FileFormat.Jpeg ? "1" : "0"));

            using var inputStream = await Api.Default.Download(downloadLink.ThrowIfError());
            using var file = File.Open(task.FSNewInputFile(format), FileMode.Create, FileAccess.Write);
            await inputStream.CopyToAsync(file, cancellationToken);
        }
    }
    public async ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(task.FSOutputDirectory());
        if (task.OutputFiles.Count != 0)
            files = task.OutputFiles.OrderByDescending(x => x.Format).Select(x => x.Path).ToArray();

        // example: files=["input.g.jpg", "input.t.jpg"] returns "input."
        var commonprefix = files.Select(Path.GetFileNameWithoutExtension).Cast<string>().Aggregate((seed, z) => string.Join("", seed.TakeWhile((v, i) => z.Length > i && v == z[i])));
        if (commonprefix.EndsWith('.'))
            commonprefix = commonprefix[..^1];

        task.LogInfo($"[MPlus] Uploading {files.Length} files with common prefix '{commonprefix}': {string.Join(", ", files.Select(Path.GetFileName))}");

        foreach (var file in files)
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
}
