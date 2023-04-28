namespace Node.Tasks.Exec;

public interface IPluginAction
{
    TaskAction Name { get; }
    ImmutableArray<PluginType> RequiredPlugins { get; }

    IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats { get; }

    Task Execute(ITaskExecutionContext context, TaskFiles files, JObject jsondata);
}
public interface IPluginAction<T> : IPluginAction
{
    Task Execute(ITaskExecutionContext context, TaskFiles files, T data);
}
public abstract class PluginAction<T> : IPluginAction<T>
{
    public abstract TaskAction Name { get; }
    public abstract ImmutableArray<PluginType> RequiredPlugins { get; }

    public abstract IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats { get; }
    protected abstract OperationResult ValidateOutputFiles(TaskFilesCheckData files, T data);

    public abstract Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, T data);

    public Task Execute(ITaskExecutionContext context, TaskFiles files, JObject jsondata) =>
        Execute(context, files, jsondata.ToObject<T>().ThrowIfNull($"Could not deserialize task data: {jsondata}"));
    public async Task Execute(ITaskExecutionContext context, TaskFiles files, T data)
    {
        context.LogInfo($"Validating input files");
        validateInputFilesThrow(context, files.InputFiles);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        context.LogInfo($"Executing {Name}");
        await ExecuteUnchecked(context, files, data).ConfigureAwait(false);
        context.LogInfo($"Execution {Name} completed in {sw.Elapsed}");

        context.LogInfo($"Validating result");
        validateOutputFilesThrow(context, files, data);

        context.LogInfo($"Completed");


        void validateOutputFilesThrow(ITaskExecutionContext context, TaskFilesCheckData files, T data) =>
            ValidateOutputFiles(files, data)
                .Next(() => OperationResult.WrapException(() => files.OutputFiles.ValidateFileList("output")))
                .ThrowIfError(message => new TaskFailedException($"Output file validation failed: {message}"));

        void validateInputFilesThrow(ITaskExecutionContext context, ReadOnlyTaskFileList files) =>
            TaskRequirement.EnsureFormats(files, "input", InputFileFormats)
                .Next(() => OperationResult.WrapException(() => files.ValidateFileList("input")))
                .ThrowIfError(message => new TaskFailedException($"Input file validation failed: {message}"));
    }
}