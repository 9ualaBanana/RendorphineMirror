namespace Node.Tasks.Models;

public class DownloadLinkTaskInfo
{
    public static async ValueTask<string> LinkDownload(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (DownloadLinkTaskInputInfo) task.Input;

        var fformat = TaskList.GetAction(task.Info).FileFormat;
        var format = fformat.ToString().ToLowerInvariant();

        var dir = task.FSDataDirectory();
        Directory.CreateDirectory(dir);

        var fileName = Path.Combine(dir, $"input.{format}");
        using (var inputStream = await Api.Download(info.Url))
        using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write))
            await inputStream.CopyToAsync(file, cancellationToken);

        return fileName;
    }

}
