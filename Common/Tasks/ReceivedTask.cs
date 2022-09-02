using System.Diagnostics.CodeAnalysis;

namespace Common.Tasks;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally) : ITask
{
    string ILoggable.LogName => $"Task {Id}";

    public string Action => Info.TaskType;

    // 0-1
    public double Progress = 0;
    public string? InputFile;


    public ITaskInputInfo Input => _Input ??= TaskInputOutputInfo.DeserializeInput(Info.Input);
    ITaskInputInfo? _Input;
    public ITaskOutputInfo Output => _Output ??= TaskInputOutputInfo.DeserializeOutput(Info.Input);
    ITaskOutputInfo? _Output;

    public static string GenerateLocalId() => "local_" + Guid.NewGuid();


    public string FSDataDirectory() => Path.Combine(Init.TaskFilesDirectory, Id);
    public string FSOutputDirectory() => Path.Combine(FSDataDirectory(), "output");
    public string FSExecutionInfo() => Path.Combine(FSOutputDirectory(), "info.json");

    [MemberNotNull("InputFile")]
    public string FSOutputFile() => Path.Combine(FSOutputDirectory(), Path.GetFileName(InputFile.ThrowIfNull("Task input file path was not provided")));
}