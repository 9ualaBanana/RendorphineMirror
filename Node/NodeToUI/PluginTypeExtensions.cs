namespace NodeToUI;

public static class PluginTypeExtensions
{
    public static string? GetName(this PluginType type) =>
        NodeGlobalState.Instance.Software.Value.GetValueOrDefault(type.ToString())?.VisualName ?? type.ToString();
}
