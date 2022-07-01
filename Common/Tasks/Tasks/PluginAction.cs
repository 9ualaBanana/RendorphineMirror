namespace Common.Tasks.Tasks;

public interface IPluginAction
{
    PluginType Type { get; }
    string Name { get; }

    ValueTask<IPluginActionData> CreateData(CreateTaskData data);
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

    public async ValueTask<IPluginActionData> CreateData(CreateTaskData data) => await T.CreateDefault(data).ConfigureAwait(false);
}