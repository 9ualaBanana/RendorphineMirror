﻿using Common;

namespace Transport.Upload;

public abstract class UploadSessionData
{
    public readonly string Endpoint;
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


    protected UploadSessionData(string url, string filePath)
        : this(url, new FileInfo(filePath))
    {
    }

    protected UploadSessionData(string url, FileInfo file)
    {
        Endpoint = url;
        File = file;
    }


    internal abstract HttpContent HttpContent { get; }
}

public class UserUploadSessionData : UploadSessionData
{
    public UserUploadSessionData(string url, string filePath)
    : this(url, new FileInfo(filePath))
    {
    }

    public UserUploadSessionData(string url, FileInfo file)
        : base($"{Path.TrimEndingDirectorySeparator(url)}/initupload", file)
    {
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
    readonly string? _sessionId;


    public MPlusUploadSessionData(string filePath, string? sessionId = default) : this(new FileInfo(filePath), sessionId)
    {
    }

    public MPlusUploadSessionData(FileInfo file, string? sessionId = default) : base($"{Api.TaskManagerEndpoint}/initselfmpoutput", file)
    {
        _sessionId = sessionId;
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
    public readonly string TaskId;
    public readonly string? Postfix;


    public MPlusTaskResultUploadSessionData(string filePath, string taskId, string? postfix)
        : this(new FileInfo(filePath), taskId, postfix)
    {
    }

    public MPlusTaskResultUploadSessionData(FileInfo file, string taskId, string? postfix)
        : base($"{Api.TaskManagerEndpoint}/initmptaskoutput", file)
    {
        TaskId = taskId;
        Postfix = postfix;
    }


    internal override FormUrlEncodedContent HttpContent
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