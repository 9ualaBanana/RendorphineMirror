namespace Node.Tasks.Watching;

public class MPlusWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.MPlus;
    public readonly string Directory;

    public MPlusWatchingTaskOutputInfo(string directory) => Directory = directory;

    public ITaskOutputInfo CreateOutput(string file) => new MPlusTaskOutputInfo(file, Directory);
}
