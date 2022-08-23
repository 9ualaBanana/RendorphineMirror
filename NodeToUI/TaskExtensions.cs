namespace NodeToUI;

public static class TaskExtensions
{
    public static Plugin GetInstance(this PluginType type) => NodeGlobalState.Instance.GetPluginInstance(type);
    public static PluginType GetPlugin(this ReceivedTask task) => NodeGlobalState.Instance.GetPluginTypeFromAction(task.Action);
}
