using Common;
using Common.Tasks;

namespace Transport.Upload;

public abstract class UploadSessionData
{
    public abstract string Endpoint { get; }
    public abstract FileInfo File { get; }
    /// <summary>
    /// Persists the same GUID suffix over multiple uses of the file name.
    /// </summary>
    /// <remarks>
    /// Without that GUID persistence file name would differ for each property/method call where it's used (e.g. HttpContent).
    /// </remarks>
    internal string _FileNameWithGuid => _fileNameWithGuid ??= File.Name.WithGuid();
    string? _fileNameWithGuid;
    protected string MimeType => MimeTypes.GetMimeType(File.Name);


    protected UploadSessionData() { }


    internal abstract HttpContent HttpContent { get; }
}

public class UserUploadSessionData : UploadSessionData
{
    public override string Endpoint { get; }
    public override FileInfo File { get; }

    public UserUploadSessionData(string url, string filePath)
    : this(url, new FileInfo(filePath))
    {
    }

    public UserUploadSessionData(string url, FileInfo file)
    {
        Endpoint = $"{Path.TrimEndingDirectorySeparator(url)}/initupload";
        File = file;
    }


    internal override FormUrlEncodedContent HttpContent => new(
        new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["name"] = _FileNameWithGuid,
            ["size"] = File.Length.ToString(),
            ["extension"] = File.Extension,
        });
}

public class MPlusUploadSessionData : UploadSessionData
{
    public override string Endpoint { get; }
    public override FileInfo File { get; }
    readonly string? _sessionId;


    public MPlusUploadSessionData(string filePath, string? sessionId = default) : this(new FileInfo(filePath), sessionId)
    {
    }

    public MPlusUploadSessionData(FileInfo file, string? sessionId = default)
    {
        _sessionId = sessionId;
        Endpoint = $"{Api.TaskManagerEndpoint}/initselfmpoutput";
        File = file;
    }


    internal override FormUrlEncodedContent HttpContent => new(new Dictionary<string, string>
    {
        ["sessionid"] = _sessionId ?? Settings.SessionId!,
        ["directory"] = "uploaded",
        ["fname"] = _FileNameWithGuid,
        ["fsize"] = File.Length.ToString(),
        ["mimetype"] = MimeType,
        ["lastmodified"] = File.LastWriteTimeUtc.AsUnixTimestamp(),
        ["origin"] = string.Empty
    });
}

public class MPlusTaskResultUploadSessionData : UploadSessionData
{
    public override string Endpoint => $"https://{Task.HostShard}/rphtasklauncher/initmptaskoutput";
    public override FileInfo File { get; }

    public readonly ReceivedTask Task;
    public readonly string? Postfix;


    public MPlusTaskResultUploadSessionData(string filePath, ReceivedTask task, string? postfix)
        : this(new FileInfo(filePath), task, postfix)
    {
    }

    public MPlusTaskResultUploadSessionData(FileInfo file, ReceivedTask task, string? postfix)
    {
        Task = task;
        Postfix = postfix;

        File = file;
    }


    internal override FormUrlEncodedContent HttpContent
    {
        get
        {
            var dict = new Dictionary<string, string>()
            {
                ["sessionid"] = Settings.SessionId!,
                ["taskid"] = Task.Id,
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