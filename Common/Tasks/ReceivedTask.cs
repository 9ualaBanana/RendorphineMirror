namespace Common.Tasks;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally) : ILoggable
{
    string ILoggable.LogName => $"Task {Id}";

    public string Action => Info.TaskType;
    public PluginType Plugin => NodeGlobalState.Instance.GetPluginTypeFromAction(Action);

    // 0-1
    public double Progress = 0;
    public string? InputFile;

    public static string GenerateLocalId() => "local_" + Guid.NewGuid();
}