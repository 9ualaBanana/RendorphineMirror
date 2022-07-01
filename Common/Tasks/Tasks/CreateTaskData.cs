namespace Common.Tasks.Tasks;

public readonly struct CreateTaskData
{
    public readonly string Version;
    public readonly ImmutableArray<string> Files;

    public CreateTaskData(string version, ImmutableArray<string> files)
    {
        Version = version;
        Files = files;
    }
}
