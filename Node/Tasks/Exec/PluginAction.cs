using Newtonsoft.Json.Linq;

namespace Node.Tasks.Exec;

public interface IPluginAction
{
    Type DataType { get; }
    PluginType Type { get; }
    string Name { get; }
    FileFormat FileFormat { get; }

    ValueTask<string> Execute(ReceivedTask task, string input);
    ValueTask<string> Execute(string taskid, string input, string output, JObject datajson);
}
public class PluginAction<T> : IPluginAction where T : new()
{
    Type IPluginAction.DataType => typeof(T);

    public string Name { get; }
    public PluginType Type { get; }
    public FileFormat FileFormat { get; }

    public readonly Func<TaskExecuteData, T, ValueTask<string>> ExecuteFunc;

    public PluginAction(PluginType type, string name, FileFormat fileformat, Func<TaskExecuteData, T, ValueTask<string>> func)
    {
        Type = type;
        Name = name;
        FileFormat = fileformat;
        ExecuteFunc = func;
    }

    public ValueTask<string> Execute(ReceivedTask task, string input)
    {
        var output = Path.Combine(Init.TaskFilesDirectory, task.Id, Path.GetFileNameWithoutExtension(input) + "_out" + Path.GetExtension(input));
        return Execute(task.Id, input, output, task.Info.Data);
    }
    public async ValueTask<string> Execute(string taskid, string input, string output, JObject datajson)
    {
        var data = datajson.ToObject<T>();
        if (data is null) throw new InvalidOperationException("Could not deserialize input data: " + datajson);

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var tk = new TaskExecuteData(input, output, taskid, this, Type.GetInstance());
        return await ExecuteFunc(tk, data).ConfigureAwait(false);
    }
}