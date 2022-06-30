namespace Common.Tasks.Tasks;

public interface IPluginAction
{
    PluginType Type { get; }
    string Name { get; }

    IPluginActionData CreateData(string[] files);
}
public class PluginAction<T> : IPluginAction where T : IPluginActionData<T>
{
    public PluginType Type { get; }
    public string Name { get; }

    public PluginAction(PluginType type, string name)
    {
        Type = type;
        Name = name;
    }

    public IPluginActionData CreateData(string[] files) => T.CreateDefault(files);
}