using Common;
using Common.Tasks;

namespace Transport.Upload;

public abstract class UploadSessionData
{
    public readonly Uri Endpoint;
    public readonly FileInfo File;
    /// <summary>
    /// Persists the same GUID suffix over multiple uses of the file name.
    /// </summary>
    /// <remarks>
    /// Without that GUID persistence file name would differ for each property/method call where it's used (e.g. HttpContent).
    /// </remarks>
    internal string _FileNameWithGuid => _fileNameWithGuid ??= File.Name.WithGuid();
    string? _fileNameWithGuid;
    protected string MimeType => MimeTypes.GetMimeType(File.Name);


    protected UploadSessionData(Uri uri, string filePath)
        : this(uri, new FileInfo(filePath))
    {
    }

    protected UploadSessionData(Uri uri, FileInfo file)
    {
        Endpoint = uri;
        File = file;
    }

    internal virtual async Task<HttpResponseMessage> UseToRequestUploadSessionInfoAsyncUsing(
        HttpClient httpClient,
        CancellationToken cancellationToken) =>
            await httpClient.PostAsync(Endpoint, HttpContent, cancellationToken).ConfigureAwait(false);

    protected abstract HttpContent HttpContent { get; }
}

public class UserUploadSessionData : UploadSessionData
{
    public UserUploadSessionData(Uri baseUri, string filePath)
    : this(baseUri, new FileInfo(filePath))
    {
    }

    public UserUploadSessionData(Uri baseUri, FileInfo file)
        : base(new Uri(baseUri, "initupload"), file)
    {
    }


    protected override FormUrlEncodedContent HttpContent => new(
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
    readonly string? _sessionId;


    public MPlusUploadSessionData(string filePath, string? sessionId = default)
        : this(new FileInfo(filePath), sessionId)
    {
    }

    public MPlusUploadSessionData(FileInfo file, string? sessionId = default)
        : base(new Uri(new Uri(Api.TaskManagerEndpoint), "initselfmpoutput"), file)
    {
        _sessionId = sessionId;
    }


    protected override FormUrlEncodedContent HttpContent => new(new Dictionary<string, string>
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
    readonly ITaskApi _taskApi;
    readonly string? _postfix;


    public MPlusTaskResultUploadSessionData(string filePath, ITaskApi taskApi, string? postfix)
        : this(new FileInfo(filePath), taskApi, postfix)
    {
    }

    public MPlusTaskResultUploadSessionData(FileInfo file, ITaskApi taskApi, string? postfix)
        : base(new Uri(new Uri($"https://{taskApi.HostShard}"), "rphtasklauncher/initmptaskoutput"), file)
    {
        _taskApi = taskApi;
        _postfix = postfix;
    }

    internal override Task<HttpResponseMessage> UseToRequestUploadSessionInfoAsyncUsing(
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        return base.UseToRequestUploadSessionInfoAsyncUsing(httpClient, cancellationToken);
    }

    protected override FormUrlEncodedContent HttpContent
    {
        get
        {
            var dict = new Dictionary<string, string>()
            {
                ["sessionid"] = Settings.SessionId!,
                ["taskid"] = _taskApi.Id,
                ["fsize"] = File.Length.ToString(),
                ["mimetype"] = MimeType,
                ["lastmodified"] = File.LastWriteTimeUtc.AsUnixTimestamp(),
                ["origin"] = string.Empty,
            };

            if (_postfix is not null) dict["postfix"] = _postfix;
            return new(dict);
        }
    }
}