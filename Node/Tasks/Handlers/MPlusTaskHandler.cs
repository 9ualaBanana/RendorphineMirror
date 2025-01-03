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
            var downloadLink = await Api.ApiGet<string>($"{Api.TaskManagerEndpoint}/gettaskinputdownloadlink", "link", "get download link",
                ("sessionid", Settings.SessionId!), ("taskid", task.Id), ("format", format.ToString().ToLowerInvariant()), ("original", format == FileFormat.Jpeg ? "1" : "0")).ConfigureAwait(false);

            using var inputStream = await Api.Download(downloadLink.ThrowIfError());
            using var file = File.Open(task.FSNewInputFile(format), FileMode.Create, FileAccess.Write);
            await inputStream.CopyToAsync(file, cancellationToken);
        }
    }
    public async ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken)
    {
        foreach (var file in Directory.GetFiles(task.FSOutputDirectory()))
            await PacketsTransporter.UploadAsync(new MPlusTaskResultUploadSessionData(file, task.Id, postfix: Path.GetFileNameWithoutExtension(file)), cancellationToken: cancellationToken);
    }

    public ValueTask<bool> CheckCompletion(DbTaskFullState task) => ValueTask.FromResult(task.State == TaskState.Output && ((MPlusTaskOutputInfo) task.Output).IngesterHost is not null);
}
