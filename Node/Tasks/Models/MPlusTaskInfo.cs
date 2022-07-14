namespace Node.Tasks.Models;

public record MPlusTaskInputInfo(string Iid) : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;

    public async ValueTask<string> Download(ReceivedTask task)
    {
        var RequestOptions = new RequestOptions();

        var format = "mov";
        var downloadLink = await Api.ApiGet<string>($"{Api.TaskManagerEndpoint}/gettaskinputdownloadlink", "link", "get download link",
            ("sessionid", Settings.SessionId!), ("taskid", task.Id), ("format", format), ("original", format == "jpg" ? "1" : "0")).ConfigureAwait(false);

        var dir = Path.Combine(Init.TaskFilesDirectory, task.Id);
        Directory.CreateDirectory(dir);

        var fileName = Path.Combine(dir, $"input.{format}");
        using (var inputStream = await Api.Download(downloadLink.ThrowIfError()))
        using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write))
            await inputStream.CopyToAsync(file, RequestOptions.CancellationToken);

        return fileName;
    }

    public ValueTask Upload() => ValueTask.CompletedTask;
}
public record MPlusTaskOutputInfo(string Name, string Directory) : ITaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;

    public async ValueTask Upload(ReceivedTask task, string file)
    {
        var packetsTransporter = new PacketsTransporter(task.RequestOptions);
        await packetsTransporter.UploadAsync(file, task.Id);
    }
}