using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon.Tasks;

public record TaskInfo(TaskObject Object, ITaskInputInfo Input, ITaskOutputInfo Output, JObject Data, TaskPolicy LaunchPolicy = TaskPolicy.AllNodes, string OriginGuid = "")
{
    [JsonIgnore] public string TaskType => Data.Property("type", StringComparison.OrdinalIgnoreCase)!.Value.Value<string>()!;
}
public record TaskServer(string Host, string Userid, string Nickname);
public record DbTaskFullState(string Id, TaskInfo Info) : TaskBase(Id, Info), ILoggable
{
    string ILoggable.LogName => $"PTask {Id}";

    public long Registered { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}