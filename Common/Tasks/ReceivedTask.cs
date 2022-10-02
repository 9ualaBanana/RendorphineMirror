using Newtonsoft.Json;

namespace Common.Tasks;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally) : ILoggable
{
    string ILoggable.LogName => $"Task {Id}";

    public string Action => Info.TaskType;

    // 0-1
    public double Progress = 0;
    public TaskState State = TaskState.Queued;

    public readonly List<FileWithFormat> InputFiles = new();
    public readonly List<FileWithFormat> OutputFiles = new();

    public ITaskInputInfo Input => Info.Input;
    public ITaskOutputInfo Output => Info.Output;
    [JsonIgnore] public virtual bool IsFromSameNode => ExecuteLocally || Info.LaunchPolicy == TaskPolicy.SameNode;

    public static string GenerateLocalId() => "local_" + Guid.NewGuid();


    string FSDataDirectory() => FSDataDirectory(Id);
    public string FSOutputDirectory() => FSOutputDirectory(Id);
    public string FSInputDirectory() => FSInputDirectory(Id);

    string FSPlacedDataDirectory() => FSPlacedDataDirectory(Id);
    public string FSPlacedResultsDirectory() => FSPlacedResultsDirectory(Id);
    public string FSPlacedSourcesDirectory() => FSPlacedSourcesDirectory(Id);

    static string FSDataDirectory(string id) => DirectoryCreated(Path.Combine(Init.TaskFilesDirectory, id));
    public static string FSOutputDirectory(string id) => DirectoryCreated(Path.Combine(FSDataDirectory(id), "output"));
    public static string FSInputDirectory(string id) => DirectoryCreated(Path.Combine(FSDataDirectory(id), "input"));

    static string FSPlacedDataDirectory(string id) => DirectoryCreated(Path.Combine(Path.Combine(Init.PlacedTaskFilesDirectory, id)));
    public static string FSPlacedResultsDirectory(string id) => DirectoryCreated(Path.Combine(Path.Combine(FSPlacedDataDirectory(id), "results")));
    public static string FSPlacedSourcesDirectory(string id) => DirectoryCreated(Path.Combine(Path.Combine(FSPlacedDataDirectory(id), "sources")));


    public string FSInputFile() => InputFiles.Single().Path;
    public string FSInputFile(FileFormat format) => InputFiles.First(x => x.Format == format).Path;
    public string FSOutputFile(FileFormat format) => OutputFiles.First(x => x.Format == format).Path;
    public string? TryFSOutputFile(FileFormat format) => OutputFiles.FirstOrDefault(x => x.Format == format)?.Path;

    [Obsolete("Use FSOutputDirectory instead")]
    public string FSOutputFile() => Directory.GetFiles(FSOutputDirectory()).First();

    public string GetTempFileName(string extension)
    {
        if (!extension.StartsWith('.')) extension = "." + extension;

        var tempdir = DirectoryCreated(Path.Combine(FSDataDirectory(), "temp", Id));
        while (true)
        {
            var file = Path.Combine(tempdir, Guid.NewGuid().ToString() + extension);
            if (File.Exists(file)) continue;

            return file;
        }
    }


    static string DirectoryCreated(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }
}