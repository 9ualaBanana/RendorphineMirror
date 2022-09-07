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


    public string FSDataDirectory() => DirectoryCreated(Path.Combine(Init.TaskFilesDirectory, Id));
    public string FSOutputDirectory() => DirectoryCreated(Path.Combine(FSDataDirectory(), "output"));
    public string FSInputDirectory() => DirectoryCreated(Path.Combine(FSDataDirectory(), "input"));

    [MemberNotNull("InputFile")]
    public string FSOutputFile() => Path.Combine(FSOutputDirectory(), Path.GetFileName(InputFile.ThrowIfNull("Task input file path was not provided")));


    static string DirectoryCreated(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }
}