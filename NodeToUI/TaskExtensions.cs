namespace NodeToUI;

public static class TaskExtensions
{
    public static Plugin GetInstance(this PluginType type) => NodeGlobalState.Instance.GetPluginInstance(type);
    public static Plugin GetInstance(this PluginType type, string version) => NodeGlobalState.Instance.GetPluginInstance(type, version);

    public static PluginType GetPlugin(this TaskBase task) => NodeGlobalState.Instance.GetPluginTypeFromAction(task.Action);
}
