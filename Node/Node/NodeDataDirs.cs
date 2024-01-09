namespace Node;

[AutoRegisteredService(true)]
public class NodeDataDirs
{
    public required DataDirs Dirs { get; init; }
    public required SettingsInstance Settings { get; init; }

    public string TaskDataDirectory() => Directories.DirCreated(string.IsNullOrWhiteSpace(Settings.TaskProcessingDirectory.Value) ? Path.Combine(Dirs.Data, "tasks") : Settings.TaskProcessingDirectory.Value);
    public string TaskDataDirectory(string id) => Directories.DirCreated(TaskDataDirectory(), id);
    public string TaskOutputDirectory(string id, string? add = null) => Directories.DirCreated(TaskDataDirectory(id), "output" + add);
    public string TaskInputDirectory(string id) => Directories.DirCreated(TaskDataDirectory(id), "input");

    public string PlacedTaskDataDirectory() => Directories.DirCreated(Dirs.Data, "ptasks");
    //public string PlacedDataDirectory(string id) => Directories.DirCreated(PlacedTaskDataDirectory(Dirs), id);
    //public string PlacedResultsDirectory(string id) => Directories.DirCreated(PlacedDataDirectory(Dirs, id), "results");
    //public string PlacedSourcesDirectory(string id) => Directories.DirCreated(PlacedDataDirectory(Dirs, id), "sources");
}
