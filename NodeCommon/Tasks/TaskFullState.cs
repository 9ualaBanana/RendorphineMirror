namespace NodeCommon.Tasks;

public record TaskInfo(TaskObject Object, ITaskInputInfo Input, ITaskOutputInfo Output, JObject Data, TaskPolicy LaunchPolicy = TaskPolicy.AllNodes, string OriginGuid = "", ImmutableArray<JObject>? Next = default)
{
    [JsonIgnore] public string FirstTaskType => GetTaskType(Data);

    public static string GetTaskType(JObject data) => data.Property("type", StringComparison.OrdinalIgnoreCase)!.Value.Value<string>()!;
}
public record TaskServer(string Host, string Userid, string Nickname);
public record DbTaskFullState(string Id, TaskInfo Info) : TaskBase(Id, Info), ILoggable
{
    protected override string LogName => $"PTask";

    public long Registered { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}