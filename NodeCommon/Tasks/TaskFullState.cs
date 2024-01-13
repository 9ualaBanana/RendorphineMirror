namespace NodeCommon.Tasks;

public record TaskInfo(TaskObject Object, ITaskOutputInfo Output, JObject Data, TaskPolicy LaunchPolicy = TaskPolicy.AllNodes, string OriginGuid = "", ImmutableArray<JObject>? Next = default)
{
    public ITaskInputInfo? Input { get; init; }
    public IReadOnlyList<ITaskInputInfo>? Inputs { get; init; }
    public ulong Registered { get; init; }
    public string? UserName { get; init; }

    [JsonIgnore] public string FirstTaskType => GetTaskType(Data);

    public static string GetTaskType(JObject data) => data.Property("type", StringComparison.OrdinalIgnoreCase)!.Value.Value<string>()!;
}
public record TaskServer(string Host, string Userid, string Nickname);
public record DbTaskFullState(string Id, TaskInfo Info) : TaskBase(Id, Info)
{
    public long Registered { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}
