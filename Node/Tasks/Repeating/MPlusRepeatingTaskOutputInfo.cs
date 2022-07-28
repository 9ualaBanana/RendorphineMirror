namespace Node.Tasks.Repeating;

public class MPlusRepeatingTaskOutputInfo : IRepeatingTaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;
    public readonly string Directory;

    public MPlusRepeatingTaskOutputInfo(string directory) => Directory = directory;

    public ITaskOutputInfo CreateOutput(string file) => new MPlusTaskOutputInfo(file, Directory);
}
