namespace Common.Tasks;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally) : ITask
{
    string ILoggable.LogName => $"Task {Id}";

    public string Action => Info.TaskType;

    // 0-1
    public double Progress = 0;
    public string? InputFile;

    public static string GenerateLocalId() => "local_" + Guid.NewGuid();


    public string FSDataDirectory() => Path.Combine(Init.TaskFilesDirectory, Id);
    public string FSOutputDirectory() => Path.Combine(FSDataDirectory(), "output");
    public string FSOutputFile() => Path.Combine(FSOutputDirectory(), Path.GetFileName(InputFile.ThrowIfNull("Task input file path was not provided")));
    public string FSExecutionInfo() => Path.Combine(FSOutputDirectory(), "info.json");
}