namespace Node.Tasks.Exec;

public interface IPluginActionInfo
{
    Type InputType { get; }
    Type DataType { get; }

    TaskAction Name { get; }
    ImmutableArray<PluginType> RequiredPlugins { get; }

    Task<object> Execute(ILifetimeScope container, object input, JObject data);
}
public abstract class PluginActionInfo<TInput, TOutput, TData> : IPluginActionInfo where TOutput : notnull
{
    Type IPluginActionInfo.InputType => typeof(TInput);
    Type IPluginActionInfo.DataType => typeof(TData);
    public abstract TaskAction Name { get; }
    public abstract ImmutableArray<PluginType> RequiredPlugins { get; }
    protected abstract Type ExecutorType { get; }

    protected abstract void ValidateInput(TInput input, TData data);
    protected abstract void ValidateOutput(TInput input, TData data, TOutput output);

    async Task<object> IPluginActionInfo.Execute(ILifetimeScope container, object input, JObject data) =>
        await Execute(
            container,
            (TInput) input,
            data.ToObject<TData>().ThrowIfNull($"Could not deserialize task data: {data}")
        );

    public async Task<TOutput> Execute(ILifetimeScope container, TInput input, TData data)
    {
        var logger = container.ResolveLogger(this);
        using var _logscope = logger.BeginScope("Execution");

        logger.LogInformation($"Validating input");
        try { ValidateInput(input, data); }
        catch (Exception ex) { throw new TaskFailedException($"Task input validation failed: {ex.Message}", ex); }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation($"Executing {Name}");

        using var ctx = container.ResolveForeign<ExecutorBase>(ExecutorType, out var executor);
        var output = await executor.ExecuteUnchecked(input, data).ConfigureAwait(false);
        executor.ProgressSetter.Set(1);
        logger.LogInformation($"Execution {Name} completed in {sw.Elapsed}");

        logger.LogInformation($"Validating output");
        try { ValidateOutput(input, data, output); }
        catch (Exception ex) { throw new TaskFailedException($"Task output validation failed: {ex.Message}", ex); }

        logger.LogInformation($"Completed");
        return output;
    }


    protected abstract class ExecutorBase
    {
        public required ITaskProgressSetter ProgressSetter { get; init; }
        public required PluginList PluginList { get; init; }
        public required ILogger<TData> Logger { get; init; }

        public abstract Task<TOutput> ExecuteUnchecked(TInput input, TData data);
    }
}