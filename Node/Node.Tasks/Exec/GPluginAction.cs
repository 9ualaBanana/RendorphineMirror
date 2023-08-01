namespace Node.Tasks.Exec;

public interface IGPluginAction
{
    Type DataType { get; }
    TaskAction Name { get; }
    ImmutableArray<PluginType> RequiredPlugins { get; }

    Task<object> Execute(object input, JObject data);
}
public interface IGPluginAction<TInput, TOutput, TData> : IGPluginAction where TOutput : notnull
{
    Task<TOutput> Execute(TInput input, TData data);
}
public abstract class GPluginAction<TInput, TOutput, TData> : IGPluginAction<TInput, TOutput, TData> where TOutput : notnull
{
    Type IGPluginAction.DataType => typeof(TData);
    public abstract TaskAction Name { get; }
    public abstract ImmutableArray<PluginType> RequiredPlugins { get; }

    public required IProgressSetter ProgressSetter { get; init; }
    public required PluginList PluginList { get; init; }
    public required ILogger<TData> Logger { get; init; }

    public abstract Task<TOutput> ExecuteUnchecked(TInput input, TData data);

    async Task<object> IGPluginAction.Execute(object input, JObject data) =>
        await Execute(
            (TInput) input,
            data.ToObject<TData>().ThrowIfNull($"Could not deserialize task data: {data}")
        );

    public async Task<TOutput> Execute(TInput input, TData data)
    {
        Logger.LogInformation($"Validating input");
        try { ValidateInput(input, data); }
        catch (Exception ex) { throw new TaskFailedException($"Task input validation failed: {ex.Message}", ex); }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Logger.LogInformation($"Executing {Name}");
        var output = await ExecuteUnchecked(input, data).ConfigureAwait(false);
        ProgressSetter.Set(1);
        Logger.LogInformation($"Execution {Name} completed in {sw.Elapsed}");

        Logger.LogInformation($"Validating output");
        try { ValidateOutput(input, data, output); }
        catch (Exception ex) { throw new TaskFailedException($"Task output validation failed: {ex.Message}", ex); }

        Logger.LogInformation($"Completed");
        return output;
    }

    protected abstract void ValidateInput(TInput input, TData data);
    protected abstract void ValidateOutput(TInput input, TData data, TOutput output);
}