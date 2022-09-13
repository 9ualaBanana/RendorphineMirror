namespace Common.Tasks.Model;

public class MPlusTaskInputInfo : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;

    public readonly string Iid;
    public readonly string? TUid;

    public MPlusTaskInputInfo(string iid, string? tuid = null)
    {
        Iid = iid;
        TUid = tuid;
    }
}
public class MPlusTaskOutputInfo : ITaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;

    [Default("output_file.mov")]
    public readonly string Name;

    [Default("output_dir")]
    public readonly string Directory;

    public readonly string? TUid;

    public MPlusTaskOutputInfo(string name, string directory, string? tuid = null)
    {
        Name = name;
        Directory = directory;
        TUid = tuid;
    }
}