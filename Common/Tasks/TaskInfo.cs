using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public record TaskInfo(TaskObject Object, JObject Input, JObject Output, JObject Data)
{
    public string TaskType => Data["type"]!.Value<string>()!;
}