namespace Node.Tasks.Models;

public class MPlusTaskInputInfo : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;
    public string Iid = "";

    public async ValueTask<string> Download(ReceivedTask task, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var format = "mov";
        var downloadLink = await Api.ApiGet<string>($"{Api.TaskManagerEndpoint}/gettaskinputdownloadlink", "link", "get download link",
            ("sessionid", Settings.SessionId!), ("taskid", task.Id), ("format", format), ("original", format == "jpg" ? "1" : "0")).ConfigureAwait(false);

        var dir = Path.Combine(Init.TaskFilesDirectory, task.Id);
        Directory.CreateDirectory(dir);

        var fileName = Path.Combine(dir, $"input.{format}");
        using (var inputStream = await Api.Download(downloadLink.ThrowIfError()))
        using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write))
            await inputStream.CopyToAsync(file, cancellationToken);

        return fileName;
    }

    public ValueTask Upload() => ValueTask.CompletedTask;
}
public class MPlusTaskOutputInfo : ITaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;
    public string Name = "output_file.mov";
    public string Directory = "output_dir";

    public async ValueTask Upload(ReceivedTask task, string file)
    {
        await PacketsTransporter.UploadAsync(new MPlusUploadSessionData(file, task.Id));
    }
}