namespace NodeToUI;

public record ReceivedTask(string Id, TaskInfo Info) : TaskBase(Id, Info), ILoggable
{
    protected override string LogName => $"RTask";

    public readonly HashSet<IUploadedFileInfo> UploadedFiles = new();

    public JObject? DownloadedInput, Result;

    [Obsolete("DELETE")] public ReadOnlyTaskFileList? InputFileList;

    [JsonConverter(typeof(JsonSettings.ConcreteConverter<TaskFileListList, IReadOnlyTaskFileListList>))]
    [Obsolete("DELETE")] public IReadOnlyTaskFileListList? OutputFileListList;
}