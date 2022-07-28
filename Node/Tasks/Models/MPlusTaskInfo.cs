using Node.P2P.Upload;

namespace Node.Tasks.Models;

public class MPlusTaskInputInfo : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;
    public readonly string Iid;

    private MPlusTaskInputInfo() => Iid = "";
    public MPlusTaskInputInfo(string iid) => Iid = iid;

    public async ValueTask<string> Download(ReceivedTask task, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var fformat = TaskList.Get(task.Info).FileFormat;
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

    public ValueTask Upload() => ValueTask.CompletedTask;
}
public class MPlusTaskOutputInfo : ITaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;
    public readonly string Name;
    public readonly string Directory;

    private MPlusTaskOutputInfo()
    {
        Name = "output_file.mov";
        Directory = "output_dir";
    }
    public MPlusTaskOutputInfo(string name, string directory)
    {
        Name = name;
        Directory = directory;
    }

    public async ValueTask Upload(ReceivedTask task, string file)
    {
        await PacketsTransporter.UploadAsync(new MPlusUploadSessionData(file, task.Id));
    }
}