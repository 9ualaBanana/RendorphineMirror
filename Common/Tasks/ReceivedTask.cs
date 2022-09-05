using System.Diagnostics.CodeAnalysis;

namespace Common.Tasks;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally) : ILoggable
{
    string ILoggable.LogName => $"Task {Id}";

    public string Action => Info.TaskType;

    // 0-1
    public double Progress = 0;
    public string? InputFile;


    public ITaskInputInfo Input => _Input ??= TaskInputOutputInfo.DeserializeInput(Info.Input);
    ITaskInputInfo? _Input;
    public ITaskOutputInfo Output => _Output ??= TaskInputOutputInfo.DeserializeOutput(Info.Output);
    ITaskOutputInfo? _Output;

    public static string GenerateLocalId() => "local_" + Guid.NewGuid();


    public string FSDataDirectory() => DirectoryCreated(Path.Combine(Init.TaskFilesDirectory, Id));
    public string FSOutputDirectory() => DirectoryCreated(Path.Combine(FSDataDirectory(), "output"));
    public string FSInputDirectory() => DirectoryCreated(Path.Combine(FSDataDirectory(), "input"));

    [MemberNotNull("InputFile")]
    // [Obsolete("Use FSOutputDirectory instead")]
    public string FSOutputFile() => Path.Combine(FSOutputDirectory(), Path.GetFileName(InputFile.ThrowIfNull("Task input file path was not provided")));


    static string DirectoryCreated(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }
}