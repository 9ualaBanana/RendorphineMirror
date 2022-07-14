using Newtonsoft.Json.Linq;

namespace Common.NodeToUI;

public class TaskCreationInfo
{
    public PluginType Type = default!;
    public string Version = default!;
    public string Action = default!;
    public JObject Input = default!;
    public JObject Output = default!;
    public JObject Data = default!;
}
