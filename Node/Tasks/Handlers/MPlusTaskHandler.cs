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
        var files = Directory.GetFiles(task.FSOutputDirectory()).AsEnumerable();
        if (task.OutputFiles.Count != 0)
            files = task.OutputFiles.OrderByDescending(x => x.Format).Select(x => x.Path);

        foreach (var file in files)
        {
            var iid = await PacketsTransporter.UploadAsync(await MPlusTaskResultUploadSessionData.InitializeAsync(file, postfix: Path.GetFileNameWithoutExtension(file), task, Api.Client), cancellationToken: cancellationToken);
            task.UploadedFiles.Add(new MPlusUploadedFileInfo(iid));
        }
    }

    public ValueTask<bool> CheckCompletion(DbTaskFullState task) => ValueTask.FromResult(task.State == TaskState.Output && ((MPlusTaskOutputInfo) task.Output).IngesterHost is not null);
}
