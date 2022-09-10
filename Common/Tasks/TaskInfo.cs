using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public record TaskInfo(TaskObject Object, ITaskInputInfo Input, ITaskOutputInfo Output, JObject Data, TaskPolicy LaunchPolicy = TaskPolicy.AllNodes, string OriginGuid = "")
{
    public string TaskType => Data["type"]!.Value<string>()!;

    [JsonConstructor]
    public TaskInfo(TaskObject @object, JObject input, JObject output, JObject data, TaskPolicy launchPolicy = TaskPolicy.AllNodes, string originGuid = "")
        : this(@object, TaskModels.DeserializeInput(input), TaskModels.DeserializeOutput(output), data, launchPolicy, originGuid) { }
}