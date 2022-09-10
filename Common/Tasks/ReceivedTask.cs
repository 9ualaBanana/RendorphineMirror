using System.Diagnostics.CodeAnalysis;

namespace Common.Tasks;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally) : ILoggable
{
    string ILoggable.LogName => $"Task {Id}";

    public string Action => Info.TaskType;

    // 0-1
    public double Progress = 0;
    public string? InputFile;
    public TaskState State = TaskState.Queued;

    public ITaskInputInfo Input => Info.Input;
    public ITaskOutputInfo Output => Info.Output;

    public static string GenerateLocalId() => "local_" + Guid.NewGuid();


    public string FSDataDirectory() => FSDataDirectory(Id);
    public string FSOutputDirectory() => FSOutputDirectory(Id);
    public string FSInputDirectory() => FSInputDirectory(Id);
    public string FSResultsDirectory() => FSResultsDirectory(Id);

    public static string FSDataDirectory(string id) => DirectoryCreated(Path.Combine(Init.TaskFilesDirectory, id));
    public static string FSOutputDirectory(string id) => DirectoryCreated(Path.Combine(FSDataDirectory(id), "output"));
    public static string FSInputDirectory(string id) => DirectoryCreated(Path.Combine(FSDataDirectory(id), "input"));
    public static string FSResultsDirectory(string id) => DirectoryCreated(Path.Combine(Path.Combine(Init.ResultFilesDirectory, id)));

    [MemberNotNull("InputFile")]
    public string FSOutputFile() => Path.Combine(FSOutputDirectory(), Path.GetFileName(InputFile.ThrowIfNull("Task input file path was not provided")));


    static string DirectoryCreated(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }
}