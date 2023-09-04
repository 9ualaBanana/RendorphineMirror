using System.Runtime.CompilerServices;

namespace Node.Tasks.Exec;

public interface IFilePluginActionInfo : IPluginActionInfo
{
    IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats { get; }
}
public abstract class FilePluginActionInfo<TInput, TOutput, TData> : PluginActionInfo<TInput, TOutput, TData>, IFilePluginActionInfo
    where TInput : IReadOnlyTaskFileList
    where TOutput : notnull
{
    public abstract IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats { get; }

    protected sealed override void ValidateInput(TInput input, TData data) =>
        input.ValidateInput(InputFileFormats);
}
public abstract class FilePluginActionInfo<TInput, TData> : FilePluginActionInfo<TInput, TaskFileOutput, TData>
    where TInput : IReadOnlyTaskFileList
{
    protected abstract OperationResult ValidateOutputFiles(TaskFilesCheckData files, TData data);

    protected sealed override void ValidateOutput(TInput input, TData data, TaskFileOutput output)
    {
        input.AssertListValid("output");

        if (output.Count == 0)
            validate(new TaskFilesCheckData(input, new ReadOnlyTaskFileList(Enumerable.Empty<FileWithFormat>())));
        else
            foreach (var outfiles in output)
                validate(new TaskFilesCheckData(input, outfiles));


        void validate(TaskFilesCheckData input)
        {
            ValidateOutputFiles(input, data)
                .Next(() => OperationResult.WrapException(() => input.OutputFiles.ValidateFileList("output")))
                .ThrowIfError(message => new TaskFailedException($"Output file validation failed: {message}"));
        }
    }
}
public abstract class FilePluginActionInfo<TData> : FilePluginActionInfo<TaskFileInput, TData> { }

public static class FilePluginActionExtensions
{
    /// <inheritdoc cref="AssertListValid(IReadOnlyTaskFileListList?, string?)"/>
    public static void AssertListValid([NotNull] this IReadOnlyTaskFileList? files, [CallerArgumentExpression(nameof(files))] string? type = null)
    {
        if (files is null)
            throw new TaskValidationException($"Task {type} file list was null or empty");

        foreach (var file in files)
            if (!File.Exists(file.Path))
                throw new TaskValidationException($"Task {type} file {file} does not exists");
    }

    /// <summary> Asserts that the provided list isn't null and all the files listed do exist </summary>
    public static void AssertListValid([NotNull] this IReadOnlyTaskFileListList? lists, [CallerArgumentExpression(nameof(lists))] string? type = null)
    {
        if (lists is null)
            throw new TaskValidationException($"Task {type} file list list was null or empty");

        foreach (var list in lists)
            list.AssertListValid(type);
    }

    public static void ValidateInput(this IReadOnlyTaskFileList input, IReadOnlyCollection<IReadOnlyCollection<FileFormat>> formats)
    {
        input.AssertListValid("input");

        TaskRequirement.EnsureFormats(input, "input", formats)
            .Next(() => OperationResult.WrapException(() => input.ValidateFileList("input")))
            .ThrowIfError(message => new TaskValidationException(message.ToString()));
    }
}