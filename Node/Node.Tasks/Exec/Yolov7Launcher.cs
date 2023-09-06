namespace Node.Tasks.Exec;

public class Yolov7LaunchInfo
{
    public required string Input { get; init; }

    /// <summary> Frame to process, 0-1 (percentage) </summary>
    public double? SeekPart { get; init; }
}
public enum Yolov7Operation { Count }

public class Yolov7Launcher : PythonWrappedLaunch
{
    protected override PluginType PluginType => PluginType.Yolov7;

    public Yolov7Launcher(ILogger<Yolov7Launcher> logger) : base(logger) { }

    async Task<T> LaunchAsync<T>(Yolov7Operation operation, Yolov7LaunchInfo info) => (await LaunchAsync(operation, info)).ToObject<T>().ThrowIfNull();
    async Task<JObject> LaunchAsync(Yolov7Operation operation, Yolov7LaunchInfo info)
    {
        var result = null as JObject;

        var launcher = await CreateLauncherAsync();
        launcher.Arguments.Add(new ArgList()
        {
            "process",
            "-i", info.Input,
            "-t", operation.ToString(),
        });

        launcher.AddOnOut(onread);

        await launcher.ExecuteAsync();
        return result ?? throw new Exception("No result");


        void onread(string line)
        {
            if (line.StartsWith("Result:", StringComparison.Ordinal))
                result = JObject.Parse(line["Result:".Length..].TrimStart());
        }
    }

    record CountResult(int Count);
    public async Task<int> CountPeopleAsync(Yolov7LaunchInfo info) => (await LaunchAsync<CountResult>(Yolov7Operation.Count, info)).Count;
}
