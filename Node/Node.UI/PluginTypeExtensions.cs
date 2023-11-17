namespace Node.UI;

public static class PluginTypeExtensions
{
    public static string? GetName(this PluginType type) =>
        NodeGlobalState.Instance.Software.Value.GetValueOrDefault(type)?.Values.FirstOrDefault()?.Name ?? type.ToString();
}
