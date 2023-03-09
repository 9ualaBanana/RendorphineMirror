using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon.Tasks;

public class TaskCreationInfo
{
    public PluginType Type = default!;
    public string? Version = default!;
    public string Action = default!;
    public JObject Input = default!;
    public JObject Output = default!;
    public JObject Data = default!;
    public TaskPolicy Policy = TaskPolicy.AllNodes;
    public TaskObject? TaskObject = null;
    public double PriceMultiplication = 1;

    [JsonConstructor]
    public TaskCreationInfo() { }

    public TaskCreationInfo(PluginType type, string? version, string action, JObject input, JObject output, JObject data, TaskPolicy policy, TaskObject? taskobj = null)
    {
        Type = type;
        Version = version;
        Action = action;
        Input = input;
        Output = output;
        Data = data;
        Policy = policy;
        TaskObject = taskobj;
    }

    public TaskCreationInfo(PluginType pluginType, string action, string? pluginVersion, ITaskInputInfo input, ITaskOutputInfo output, TaskObject taskobj)
        : this(pluginType, action, pluginVersion, input, output, new(), TaskPolicy.AllNodes, taskobj)
    {
    }

    public TaskCreationInfo(PluginType pluginType, string action, string? pluginVersion, ITaskInputInfo input, ITaskOutputInfo output, object data, TaskObject taskobj)
        : this(pluginType, action, pluginVersion, input, output, data, TaskPolicy.AllNodes, taskobj)
    {
    }

    [Obsolete("Use larger overload instead")]
    public TaskCreationInfo(PluginType pluginType, string action, string? pluginVersion, ITaskInputInfo input, ITaskOutputInfo output, object data)
        : this(pluginType, action, pluginVersion, input, output, data, TaskPolicy.AllNodes, null!) { }

    public TaskCreationInfo(PluginType pluginType, string action, string? pluginVersion, ITaskInputInfo input, ITaskOutputInfo output, object data, TaskPolicy policy, TaskObject taskobj)
    {
        Type = pluginType;
        Version = pluginVersion;
        Action = action;
        Input = JObject.FromObject(input, JsonSettings.LowercaseS);
        Output = JObject.FromObject(output, JsonSettings.LowercaseS);
        Data = JObject.FromObject(data, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", action);
        Policy = policy;
        TaskObject = taskobj;
    }
}
