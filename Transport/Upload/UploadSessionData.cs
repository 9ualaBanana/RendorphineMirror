using Common;
using NodeCommon;
using NodeCommon.Tasks;
using Transport.Models;

namespace Transport.Upload;

public abstract class UploadSessionData
{
    protected readonly Uri Endpoint;
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

    internal virtual async Task<UploadSession> UseToRequestUploadSessionAsyncUsing(
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var httpResponse = await httpClient.PostAsync(Endpoint, HttpContent, cancellationToken).ConfigureAwait(false);
        var response = await Api.GetJsonFromResponseIfSuccessfulAsync(httpResponse);
        return new(
            this,
            (string)response["fileid"]!,
            (string)response["host"]!,
            (long)response["uploadedbytes"]!,
            response["uploadedchunks"]!.ToObject<UploadedPacket[]>()!,
            httpClient,
            cancellationToken);
    }

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

    /// <inheritdoc cref="InitializeAsync(FileInfo, string?, ITaskApi, HttpClient, string?)"/>
    public static async ValueTask<MPlusTaskResultUploadSessionData> InitializeAsync(
        string filePath,
        string? postfix,
        ITaskApi taskApi,
        HttpClient httpClient,
        string? sessionId = default) =>
            await InitializeAsync(new FileInfo(filePath), postfix, taskApi, httpClient, sessionId);

    /// <summary>
    /// Updates <see cref="ITaskApi.HostShard"/> of <paramref name="taskApi"/> and initializes <see cref="MPlusTaskResultUploadSessionData"/>.
    /// </summary>
    public static async ValueTask<MPlusTaskResultUploadSessionData> InitializeAsync(
        FileInfo file,
        string? postfix,
        ITaskApi taskApi,
        HttpClient httpClient,
        string? sessionId = default)
    {
        var api = Apis.Default with { SessionId = sessionId ?? Settings.SessionId, Api = Api.Default with { Client = httpClient } };
        await api.WithSessionId(sessionId ?? Settings.SessionId).UpdateTaskShardAsync(taskApi);
        return new(file, postfix, taskApi);
    }

    /// <inheritdoc cref="MPlusTaskResultUploadSessionData(FileInfo, string?, ITaskApi)"/>
    public MPlusTaskResultUploadSessionData(string filePath, string? postfix, ITaskApi taskApi)
        : this(new FileInfo(filePath), postfix, taskApi)
    {
    }

    /// <remarks>Must be called only if <see cref="ITaskApi.HostShard"/> of <paramref name="taskApi"/> is already updated.</remarks>
    public MPlusTaskResultUploadSessionData(FileInfo file, string? postfix, ITaskApi taskApi)
        : base(new Uri(new Uri($"https://{taskApi.HostShard}"), "rphtasklauncher/initmptaskoutput"), file)
    {
        _taskApi = taskApi;
        _postfix = postfix;
    }

    internal override async Task<UploadSession> UseToRequestUploadSessionAsyncUsing(
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var api = Apis.Default with { Api = Api.Default with { Client = httpClient } };
        var requestedUploadSessionData = (await api.ShardPost<RequestedUploadSessionData>(
            _taskApi,
            Endpoint.Segments.Last(),
            property: null,
            $"Requesting {nameof(MPlusUploadSessionData)}...",
            HttpContent)).ThrowIfError();

        return new(
            this,
            requestedUploadSessionData.FileId,
            requestedUploadSessionData.Host,
            requestedUploadSessionData.UploadedBytes,
            requestedUploadSessionData.UploadedChunks,
            httpClient,
            cancellationToken);
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