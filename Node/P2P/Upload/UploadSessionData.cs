namespace Node.P2P.Upload;

public abstract class UploadSessionData
{
    public FileInfo File;
    public string TaskId;

    protected UploadSessionData(string filePath, string taskId)
        : this(new FileInfo(filePath), taskId)
    {
    }

    protected UploadSessionData(FileInfo file, string taskId)
    {
        File = file;
        TaskId = taskId;
    }

    public abstract HttpContent HttpContent { get; }
    public abstract string Endpoint { get; }
}

public class UserUploadSessionData : UploadSessionData
{
    internal UserUploadSessionData(string filePath, string taskId)
    : this(new FileInfo(filePath), taskId)
    {
    }

    public UserUploadSessionData(FileInfo file, string taskId) : base(file, taskId)
    {
    }

    public override FormUrlEncodedContent HttpContent => new(
        new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["name"] = File.Name,
            ["size"] = File.Length.ToString(),
            ["extension"] = File.Extension
        });
    public override string Endpoint => $"https://d7e4-213-87-159-225.eu.ngrok.io/initupload";
}

public class MPlusUploadSessionData : UploadSessionData
{
    internal MPlusUploadSessionData(string filePath, string taskId)
        : this(new FileInfo(filePath), taskId)
    {
    }

    public MPlusUploadSessionData(FileInfo file, string taskId) : base(file, taskId)
    {
    }

    public override FormUrlEncodedContent HttpContent => new(
        new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["taskid"] = TaskId,
            ["fsize"] = File.Length.ToString(),
            ["mimetype"] = "video/mp4",
            ["lastmodified"] = File.LastWriteTimeUtc.ToBinary().ToString(),
            ["origin"] = string.Empty
        });
    public override string Endpoint => $"{Api.TaskManagerEndpoint}/initmptaskoutput";
}