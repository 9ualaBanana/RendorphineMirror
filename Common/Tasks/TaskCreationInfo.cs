using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public class TaskCreationInfo
{
    public PluginType Type = default!;
    public string? Version = default!;
    public string Action = default!;
    public JObject Input = default!;
    public JObject Output = default!;
    public JObject Data = default!;
    public bool ExecuteLocally = false;

    public TaskCreationInfo() { }
    public TaskCreationInfo(PluginType type, string? version, string action, JObject input, JObject output, JObject data, bool executeLocally)
    {
        Type = type;
        Version = version;
        Action = action;
        Input = input;
        Output = output;
        Data = data;
        ExecuteLocally = executeLocally;
    }
    public TaskCreationInfo(PluginType pluginType, string action, string? pluginVersion, ITaskInputInfo input, ITaskOutputInfo output, object data, bool executeLocally = false)
    {
        Type = pluginType;
        Version = pluginVersion;
        Action = action;
        Input = JObject.FromObject(input, JsonSettings.LowercaseS);
        Output = JObject.FromObject(output, JsonSettings.LowercaseS);
        Data = JObject.FromObject(data, JsonSettings.LowercaseIgnoreNullS).WithProperty("type", action);
        ExecuteLocally = executeLocally;
    }
}
