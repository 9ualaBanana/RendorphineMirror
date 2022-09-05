using Node.P2P.Upload;

namespace Node.Tasks.Models;

public class MPlusTaskHandler : ITaskInputHandler, ITaskOutputHandler, IPlacedTaskCompletionCheckHandler
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;

    public async ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var fformat = TaskList.GetAction(task.Info).FileFormat;
        var format = fformat.ToString().ToLowerInvariant();
        var downloadLink = await Api.ApiGet<string>($"{Api.TaskManagerEndpoint}/gettaskinputdownloadlink", "link", "get download link",
            ("sessionid", Settings.SessionId!), ("taskid", task.Id), ("format", format), ("original", fformat == FileFormat.Jpeg ? "1" : "0")).ConfigureAwait(false);

        var dir = Path.Combine(Init.TaskFilesDirectory, task.Id);
        Directory.CreateDirectory(dir);

        var fileName = Path.Combine(dir, $"input.{format}");
        using (var inputStream = await Api.Download(downloadLink.ThrowIfError()))
        using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write))
            await inputStream.CopyToAsync(file, cancellationToken);

        return fileName;
    }
    public async ValueTask UploadResult(ReceivedTask task, string file, string? postfix, CancellationToken cancellationToken)
    {
        await PacketsTransporter.UploadAsync(new MPlusUploadSessionData(file, task.Id, postfix), cancellationToken: cancellationToken);
    }

    public async ValueTask<bool> CheckCompletion(DbTaskFullState task)
    {
        var state = (await task.GetTaskStateAsync()).ThrowIfError();
        task.State = state.State;

        // not null if upload is completed
        return state.State == TaskState.Output && state.Output["ingesterhost"] is not null;
    }
}
