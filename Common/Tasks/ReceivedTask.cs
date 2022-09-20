namespace Common.Tasks;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally) : ILoggable
{
    string ILoggable.LogName => $"Task {Id}";

    public string Action => Info.TaskType;

    // 0-1
    public double Progress = 0;
    public TaskState State = TaskState.Queued;

    // input path override for local tasks
    string? InputFile, InputDirectory;

    public ITaskInputInfo Input => Info.Input;
    public ITaskOutputInfo Output => Info.Output;
    public bool IsFromSameNode => ExecuteLocally || Info.LaunchPolicy == TaskPolicy.SameNode || Info.OriginGuid == Settings.Guid;

    public static string GenerateLocalId() => "local_" + Guid.NewGuid();


    public string FSDataDirectory() => FSDataDirectory(Id);
    public string FSOutputDirectory() => FSOutputDirectory(Id);
    public string FSInputDirectory() => InputDirectory ?? FSInputDirectory(Id);

    public string FSPlacedDataDirectory() => FSPlacedDataDirectory(Id);
    public string FSPlacedResultsDirectory() => FSPlacedResultsDirectory(Id);
    public string FSPlacedSourcesDirectory() => FSPlacedSourcesDirectory(Id);

    public static string FSDataDirectory(string id) => DirectoryCreated(Path.Combine(Init.TaskFilesDirectory, id));
    public static string FSOutputDirectory(string id) => DirectoryCreated(Path.Combine(FSDataDirectory(id), "output"));
    public static string FSInputDirectory(string id) => DirectoryCreated(Path.Combine(FSDataDirectory(id), "input"));

    public static string FSPlacedDataDirectory(string id) => DirectoryCreated(Path.Combine(Path.Combine(Init.PlacedTaskFilesDirectory, id)));
    public static string FSPlacedResultsDirectory(string id) => DirectoryCreated(Path.Combine(Path.Combine(FSPlacedDataDirectory(id), "results")));
    public static string FSPlacedSourcesDirectory(string id) => DirectoryCreated(Path.Combine(Path.Combine(FSPlacedDataDirectory(id), "sources")));

    string AddDotIfNeeded(string extension) => extension.StartsWith('.') ? extension : ("." + extension);
    public string FSNewInputFile(string extension) => Path.Combine(FSInputDirectory(), "input" + AddDotIfNeeded(extension));
    public string FSNewOutputFile(string extension) => Path.Combine(FSOutputDirectory(), "output" + AddDotIfNeeded(extension));

    public string FSInputFile() => InputFile ?? Directory.GetFiles(FSInputDirectory()).Single();
    public string FSOutputFile() => Directory.GetFiles(FSOutputDirectory()).Single();

    public void SetInputFile(string file) => InputFile = file;
    public void SetInputDirectory(string dir) => InputDirectory = dir;
    public void SetInput(string path)
    {
        if (Directory.Exists(path)) InputDirectory = path;
        else InputFile = path;
    }

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