using Newtonsoft.Json.Linq;

namespace Common.NodeToUI;

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
}
