namespace Common.Tasks.Tasks;

public interface IPluginAction
{
    Type DataType { get; }
    PluginType Type { get; }
    string Name { get; }
    FileFormat FileFormat { get; }

    object CreateData();
    ValueTask<string> Execute(ReceivedTask task, string input);
}
public class PluginAction<T> : IPluginAction where T : new()
{
    Type IPluginAction.DataType => typeof(T);

    public string Name { get; }
    public PluginType Type { get; }
    public FileFormat FileFormat { get; }

    public readonly Func<string[], ReceivedTask, T, ValueTask<string[]>> ExecuteFunc;

    public PluginAction(PluginType type, string name, FileFormat fileformat, Func<string[], ReceivedTask, T, ValueTask<string[]>> func)
    {
        Type = type;
        Name = name;
        FileFormat = fileformat;
        ExecuteFunc = func;
    }

    public object CreateData() => new T();

    public async ValueTask<string> Execute(ReceivedTask task, string input)
    {
        var data = task.Info.Data.ToObject<T>();
        if (data is null) throw new InvalidOperationException("Could not deserialize input data: " + task.Info.Data);

        // var files = NodeTask.UnzipFiles(input).ToArray();

        var output = await ExecuteFunc(new[] { input }, task, data).ConfigureAwait(false);
        return output[0]; // TODO: ?????
        // return NodeTask.ZipFiles(output);
    }
}