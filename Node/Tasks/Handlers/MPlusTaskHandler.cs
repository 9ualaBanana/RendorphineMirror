using Transport.Upload;

namespace Node.Tasks.Handlers;

public class MPlusTaskHandler : ITaskInputHandler, ITaskOutputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.MPlus;
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.MPlus;

    public async ValueTask Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var fformat = TaskList.GetAction(task.Info).InputFileFormat;
        var format = fformat.ToString().ToLowerInvariant();
        var downloadLink = await Api.ApiGet<string>($"{Api.TaskManagerEndpoint}/gettaskinputdownloadlink", "link", "get download link",
            ("sessionid", Settings.SessionId!), ("taskid", task.Id), ("format", format), ("original", fformat == FileFormat.Jpeg ? "1" : "0")).ConfigureAwait(false);

        using (var inputStream = await Api.Download(downloadLink.ThrowIfError()))
        using (var file = File.Open(task.FSNewInputFile(), FileMode.Create, FileAccess.Write))
            await inputStream.CopyToAsync(file, cancellationToken);
    }
    public async ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken)
    {
        foreach (var file in Directory.GetFiles(task.FSOutputDirectory()))
            await PacketsTransporter.UploadAsync(new MPlusTaskResultUploadSessionData(file, task.Id, postfix: Path.GetFileNameWithoutExtension(file)), cancellationToken: cancellationToken);
    }

    public async ValueTask<bool> CheckCompletion(DbTaskFullState task)
    {
        var state = (await task.GetTaskStateAsync()).ThrowIfError();
        task.State = state.State;

        // not null if upload is completed
        return state.State == TaskState.Output && state.Output["ingesterhost"] is not null;
    }
}
