namespace NodeToUI;

public record ReceivedTask(string Id, TaskInfo Info) : TaskBase(Id, Info), ILoggable
{
    protected override string LogName => $"RTask";

    public readonly HashSet<IUploadedFileInfo> UploadedFiles = new();

    public ReadOnlyTaskFileList? InputFileList;

    [JsonConverter(typeof(JsonSettings.ConcreteConverter<TaskFileListList, IReadOnlyTaskFileListList>))]
    public IReadOnlyTaskFileListList? OutputFileListList;
}