namespace Node.P2P.Upload;

public abstract class UploadSessionData
{
    public readonly string Endpoint;
    public readonly FileInfo File;
    protected string MimeType => MimeTypes.GetMimeType(File.Name);


    protected UploadSessionData(string url, string filePath)
        : this(url, new FileInfo(filePath))
    {
    }

    protected UploadSessionData(string url, FileInfo file)
    {
        Endpoint = url;
        File = file;
    }


    public abstract HttpContent HttpContent { get; }
}

public class UserUploadSessionData : UploadSessionData
{
    internal UserUploadSessionData(string url, string filePath)
    : this(url, new FileInfo(filePath))
    {
    }

    public UserUploadSessionData(string url, FileInfo file) : base((url.EndsWith('/') ? url[..^1] : url) + "/initupload", file)
    {
    }


    public override FormUrlEncodedContent HttpContent => new(
        new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["name"] = File.Name.WithGuid(),
            ["size"] = File.Length.ToString(),
            ["extension"] = File.Extension,
        });
}

public class MPlusUploadSessionData : UploadSessionData
{
    public MPlusUploadSessionData(string url, string filePath) : base(url, filePath)
    {
    }


    public override FormUrlEncodedContent HttpContent => new(new Dictionary<string, string>
    {
        ["sessionid"] = Settings.SessionId!,
        ["directory"] = "uploaded",
        ["fname"] = File.Name.WithGuid(),
        ["fsize"] = File.Length.ToString(),
        ["mimetype"] = MimeType,
        ["lastmodified"] = File.LastWriteTimeUtc.AsUnixTimestamp(),
        ["origin"] = string.Empty
    });
}

public class MPlusTaskResultUploadSessionData : UploadSessionData
{
    public readonly string TaskId;
    public readonly string? Postfix;


    internal MPlusTaskResultUploadSessionData(string filePath, string taskId, string? postfix)
        : this(new FileInfo(filePath), taskId, postfix)
    {
    }

    public MPlusTaskResultUploadSessionData(FileInfo file, string taskId, string? postfix) : base($"{Api.TaskManagerEndpoint}/initmptaskoutput", file)
    {
        TaskId = taskId;
        Postfix = postfix;
    }


    public override FormUrlEncodedContent HttpContent
    {
        get
        {
            var dict = new Dictionary<string, string>()
            {
                ["sessionid"] = Settings.SessionId!,
                ["taskid"] = TaskId,
                ["fsize"] = File.Length.ToString(),
                ["mimetype"] = MimeType,
                ["lastmodified"] = File.LastWriteTimeUtc.AsUnixTimestamp(),
                ["origin"] = string.Empty,
            };

            if (Postfix is not null) dict["postfix"] = Postfix;
            return new(dict);
        }
    }
}