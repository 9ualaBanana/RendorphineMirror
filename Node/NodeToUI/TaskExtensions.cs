namespace NodeToUI;

public static class TaskExtensions
{
    public static Plugin GetInstance(this PluginType type) => NodeGlobalState.Instance.GetPluginInstance(type);
    public static Plugin GetInstance(this PluginType type, PluginVersion version) => NodeGlobalState.Instance.GetPluginInstance(type, version);
}
