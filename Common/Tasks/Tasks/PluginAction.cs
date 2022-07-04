using System.Text.Json;
using System.Text.Json.Nodes;
using Common.Tasks.Tasks.DTO;
using Newtonsoft.Json.Linq;

namespace Common.Tasks.Tasks;

public interface IPluginAction
{
    PluginType Type { get; }
    string Name { get; }

    ValueTask<IPluginActionData> CreateData(CreateTaskData data);
}
public class PluginAction<T> : IPluginAction where T : IPluginActionData
{
    public string Name { get; }
    public PluginType Type { get; }
    public readonly Func<CreateTaskData, ValueTask<T>> CreateDefaultFunc;
    public readonly Func<NodeTask<T>, ValueTask> ExecuteFunc;

    public PluginAction(PluginType type, string name, Func<CreateTaskData, ValueTask<T>> createdef, Func<NodeTask<T>, ValueTask> func)
    {
        Type = type;
        Name = name;

        CreateDefaultFunc = createdef;
        ExecuteFunc = func;
    }

    public async ValueTask<IPluginActionData> CreateData(CreateTaskData data) => await CreateDefaultFunc(data).ConfigureAwait(false);

    public IPluginActionData? Deserialize(JObject json) => json.ToObject<T>();
    public IPluginActionData? Deserialize(JsonNode json) => json.Deserialize<T>();
}