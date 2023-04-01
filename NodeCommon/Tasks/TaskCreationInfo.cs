using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon.Tasks;

public class TaskCreationInfo
{
    public string Action = default!;
    public JObject Input = default!;
    public JObject Output = default!;
    public JObject Data = default!;
    public TaskPolicy Policy = TaskPolicy.AllNodes;
    public TaskObject TaskObject = default!;
    public decimal PriceMultiplication = 1;
    public ImmutableArray<TaskSoftwareRequirement>? SoftwareRequirements;

    [JsonConstructor]
    protected TaskCreationInfo() { }

    public TaskCreationInfo(TaskAction action, ITaskInputInfo input, ITaskOutputInfo output, TaskObject taskobj)
        : this(action.ToString(), input, output, new { }, TaskPolicy.AllNodes, taskobj)
    {
    }
    
    public TaskCreationInfo(TaskAction action, ITaskInputInfo input, ITaskOutputInfo output, object data, TaskObject taskobj)
        : this(action.ToString(), input, output, data, TaskPolicy.AllNodes, taskobj)
    {
    }

    public TaskCreationInfo(string action, ITaskInputInfo input, ITaskOutputInfo output, object data, TaskPolicy policy, TaskObject taskobj)
    {
        Action = action;
        Input = JObject.FromObject(input, JsonSettings.LowercaseS);
        Output = JObject.FromObject(output, JsonSettings.LowercaseS);
        Data = JObject.FromObject(data, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", action);
        Policy = policy;
        TaskObject = taskobj;
    }
}
public class UITaskCreationInfo : TaskCreationInfo
{
    public PluginType Type = default!;
    public string? Version = default!;

    [JsonConstructor]
    public UITaskCreationInfo() { }
}
