﻿namespace NodeCommon.Tasks;

public interface ITaskApi
{
    string Id { get; }
    string? HostShard { get; set; }
}
// for use in Telegram or something
public record ApiTask(string Id, HashSet<IUploadedFileInfo> UploadedFiles) : ITaskApi
{
    public ApiTask(string Id) : this(Id, Enumerable.Empty<string>())
    {
    }

    public ApiTask(string id, IEnumerable<string> iidsOfUploadedFiles)
        : this(id, iidsOfUploadedFiles.Select(iid => new MPlusUploadedFileInfo(iid)).Cast<IUploadedFileInfo>().ToHashSet())
    {

    }

    public string? HostShard { get; set; }
}

public record ReceivedTask(string Id, TaskInfo Info) : TaskBase(Id, Info), ILoggable
{
    string ILoggable.LogName => $"Task {Id}";

    public readonly HashSet<FileWithFormat> InputFiles = new();
    public readonly HashSet<FileWithFormat> OutputFiles = new();
    public readonly HashSet<IUploadedFileInfo> UploadedFiles = new();


    public string FSInputFile() => InputFiles.Single().Path;
    public string FSInputFile(FileFormat format) => InputFiles.First(x => x.Format == format).Path;
    public string FSOutputFile(FileFormat format) => OutputFiles.First(x => x.Format == format).Path;
    public string? TryFSOutputFile(FileFormat format) => OutputFiles.FirstOrDefault(x => x.Format == format)?.Path;
}