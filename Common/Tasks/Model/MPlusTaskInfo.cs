namespace Common.Tasks.Model;

public class MPlusTaskInputInfo : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;

    public readonly string Iid;

    public MPlusTaskInputInfo(string iid) => Iid = iid;
}
public class MPlusTaskOutputInfo : ITaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;

    [Default("output_file.mov")]
    public readonly string Name;

    [Default("output_dir")]
    public readonly string Directory;

    public MPlusTaskOutputInfo(string name, string directory)
    {
        Name = name;
        Directory = directory;
    }
}