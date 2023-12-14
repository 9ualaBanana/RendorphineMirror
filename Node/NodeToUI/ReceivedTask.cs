namespace NodeToUI;

public record ReceivedTask(string Id, TaskInfo Info) : TaskBase(Id, Info)
{
    public readonly HashSet<IUploadedFileInfo> UploadedFiles = new();

    [JsonConverter(typeof(TypedArrJsonConverter))]
    public IReadOnlyList<object>? DownloadedInputs2;

    [JsonConverter(typeof(TypedJsonConverter))]
    public object? Result2;

    [Obsolete("DELETE")] public ReadOnlyTaskFileList? InputFileList;

    [JsonConverter(typeof(JsonSettings.ConcreteConverter<TaskFileListList, IReadOnlyTaskFileListList>))]
    [Obsolete("DELETE")] public IReadOnlyTaskFileListList? OutputFileListList;
}
