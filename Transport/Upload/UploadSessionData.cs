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
        var response = await Api.GetJsonIfSuccessfulAsync(httpResponse);
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
    readonly string _sessionId;

    public UserUploadSessionData(Uri baseUri, string filePath, string sessionId)
        : this(baseUri, new FileInfo(filePath), sessionId)
    {
    }

    public UserUploadSessionData(Uri baseUri, FileInfo file, string sessionId)
        : base(new Uri(baseUri, "initupload"), file)
    {
        _sessionId = sessionId;
    }


    protected override FormUrlEncodedContent HttpContent => new(
        new Dictionary<string, string>()
        {
            ["sessionid"] = _sessionId,
            ["name"] = _FileNameWithGuid,
            ["size"] = File.Length.ToString(),
            ["extension"] = File.Extension,
        });
}

public class MPlusUploadSessionData : UploadSessionData
{
    readonly string _sessionId;


    public MPlusUploadSessionData(string filePath, string sessionId)
        : this(new FileInfo(filePath), sessionId)
    {
    }

    public MPlusUploadSessionData(FileInfo file, string sessionId)
        : base(new Uri(new Uri(Api.TaskManagerEndpoint+'/'), "initselfmpoutput"), file)
    {
        _sessionId = sessionId;
    }


    protected override FormUrlEncodedContent HttpContent => new(new Dictionary<string, string>
    {
        ["sessionid"] = _sessionId,
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
    readonly string _sessionId;

    /// <inheritdoc cref="InitializeAsync(FileInfo, string?, ITaskApi, HttpClient, string?)"/>
    public static async ValueTask<MPlusTaskResultUploadSessionData> InitializeAsync(
        string filePath,
        string? postfix,
        ITaskApi taskApi,
        HttpClient httpClient,
        string sessionId) =>
            await InitializeAsync(new FileInfo(filePath), postfix, taskApi, httpClient, sessionId);

    /// <summary>
    /// Updates <see cref="ITaskApi.HostShard"/> of <paramref name="taskApi"/> and initializes <see cref="MPlusTaskResultUploadSessionData"/>.
    /// </summary>
    public static async ValueTask<MPlusTaskResultUploadSessionData> InitializeAsync(
        FileInfo file,
        string? postfix,
        ITaskApi taskApi,
        HttpClient httpClient,
        string sessionId)
    {
        var api = new Apis(Api.Default with { Client = httpClient }, sessionId);
        await api.UpdateTaskShardAsync(taskApi);
        return new(file, postfix, taskApi, sessionId);
    }

    /// <inheritdoc cref="MPlusTaskResultUploadSessionData(FileInfo, string?, ITaskApi)"/>
    public MPlusTaskResultUploadSessionData(string filePath, string? postfix, ITaskApi taskApi, string sessionId)
        : this(new FileInfo(filePath), postfix, taskApi, sessionId)
    {
    }

    /// <remarks>Must be called only if <see cref="ITaskApi.HostShard"/> of <paramref name="taskApi"/> is already updated.</remarks>
    public MPlusTaskResultUploadSessionData(FileInfo file, string? postfix, ITaskApi taskApi, string sessionId)
        : base(new Uri(new Uri($"https://{taskApi.HostShard}"), "rphtasklauncher/initmptaskoutput"), file)
    {
        _taskApi = taskApi;
        _postfix = postfix;
        _sessionId = sessionId;
    }

    internal override async Task<UploadSession> UseToRequestUploadSessionAsyncUsing(
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var api = new Apis(Api.Default with { Client = httpClient }, _sessionId);
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
                ["sessionid"] = _sessionId,
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