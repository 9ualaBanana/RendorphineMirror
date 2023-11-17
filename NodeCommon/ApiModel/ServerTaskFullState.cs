using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon.ApiModel;

public record ServerTaskFullState : DbTaskFullState
{
    public TaskServer? Server { get; set; }

    [JsonConstructor]
    public ServerTaskFullState(string id, TaskPolicy launchPolicy, TaskObject @object, ITaskOutputInfo output, JObject data, ITaskInputInfo? input = null, IReadOnlyList<ITaskInputInfo>? inputs = null)
        : base(id, new TaskInfo(@object, output, data, launchPolicy) { Input = input, Inputs = inputs }) { }
}
