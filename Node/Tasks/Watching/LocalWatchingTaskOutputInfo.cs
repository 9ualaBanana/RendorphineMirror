namespace Node.Tasks.Watching;

public class LocalWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;
    [LocalDirectory] public readonly string Directory;

    public LocalWatchingTaskOutputInfo(string directory) => Directory = directory;

    public ITaskOutputInfo CreateOutput(string file) => new UserTaskOutputInfo(Directory, file);
}
