namespace Node.Tasks.Models;

public record DownloadLinkTaskInput(DownloadLinkTaskInputInfo Info) : ITaskInput
{
    public async ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var fformat = TaskList.GetAction(task.Info).FileFormat;
        var format = fformat.ToString().ToLowerInvariant();

        var dir = task.FSDataDirectory();
        Directory.CreateDirectory(dir);

        var fileName = Path.Combine(dir, $"input.{format}");
        using (var inputStream = await Api.Download(Info.Url))
        using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write))
            await inputStream.CopyToAsync(file, cancellationToken);

        return fileName;
    }

    public ValueTask Upload() => ValueTask.CompletedTask;
}
