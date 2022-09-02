namespace Node.Tasks.Models;

public static class TaskInputOutput
{
    public static ValueTask<string> Download(ReceivedTask task, CancellationToken token = default) =>
        task.Input.Type switch
        {
            TaskInputType.MPlus => MPlusTaskInfo.Download(task, token),
            TaskInputType.DownloadLink => DownloadLinkTaskInfo.LinkDownload(task, token),
            TaskInputType.Torrent => TorrentTaskInfo.Download(task, token),

            var type => throw new InvalidOperationException($"Task input type handler for {type} was not found"),
        };
    public static ValueTask UploadResult(ReceivedTask task, string file, string? postfix, CancellationToken token = default) =>
        task.Output.Type switch
        {
            TaskOutputType.MPlus => MPlusTaskInfo.UploadResult(task, file, postfix, token),
            TaskOutputType.Torrent => TorrentTaskInfo.UploadResult(task, file, postfix, token),

            var type => throw new InvalidOperationException($"Task output type handler for {type} was not found"),
        };
}
