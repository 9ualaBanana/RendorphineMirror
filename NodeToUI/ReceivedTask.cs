namespace NodeToUI;

public record ReceivedTask(string Id, TaskInfo Info) : TaskBase(Id, Info), ILoggable
{
    string ILoggable.LogName => $"Task {Id}";

    public readonly HashSet<IUploadedFileInfo> UploadedFiles = new();

    [Newtonsoft.Json.JsonConverter(typeof(JsonSettings.ConcreteConverter<TaskFileList, IReadOnlyTaskFileList>))]
    public IReadOnlyTaskFileList? InputFileList;

    [Newtonsoft.Json.JsonConverter(typeof(JsonSettings.ConcreteConverter<TaskFileList, IReadOnlyTaskFileList>))]
    public IReadOnlyTaskFileList? OutputFileList;
}