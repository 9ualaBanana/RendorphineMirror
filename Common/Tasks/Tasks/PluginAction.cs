using System.Text.Json;
using Common.Tasks.Models;
using Common.Tasks.Tasks.DTO;
using Newtonsoft.Json.Linq;

namespace Common.Tasks.Tasks;

public interface IPluginAction
{
    PluginType Type { get; }
    string Name { get; }

    ValueTask<IPluginActionData> CreateData();
    ValueTask<string> Execute(IncomingTask task, string input);

    IPluginActionData? Deserialize(JObject json);
    IPluginActionData? Deserialize(JsonElement json);
}
public class PluginAction<T> : IPluginAction where T : IPluginActionData
{
    public string Name { get; }
    public PluginType Type { get; }
    public readonly Func<ValueTask<T>> CreateDefaultFunc;
    public readonly Func<string[], IncomingTask, T, ValueTask<string[]>> ExecuteFunc;

    public PluginAction(PluginType type, string name, Func<ValueTask<T>> createdef, Func<string[], IncomingTask, T, ValueTask<string[]>> func)
    {
        Type = type;
        Name = name;

        CreateDefaultFunc = createdef;
        ExecuteFunc = func;
    }

    public async ValueTask<IPluginActionData> CreateData() => await CreateDefaultFunc().ConfigureAwait(false);

    public IPluginActionData? Deserialize(JObject json) => json.ToObject<T>();
    public IPluginActionData? Deserialize(JsonElement json) => json.Deserialize<T>();

    public async ValueTask<string> Execute(IncomingTask task, string input)
    {
        var data = (T?) Deserialize(task.Task.Data);
        if (data is null) throw new InvalidOperationException("Could not deserialize input data: " + task.Task.Data);

        var files = NodeTask.UnzipFiles(input).ToArray();

        var output = await ExecuteFunc(files, task, data).ConfigureAwait(false);
        return NodeTask.ZipFiles(output);
    }
}