using System.Runtime.CompilerServices;

namespace Node.Tasks.Exec;

public interface IFilePluginAction : IGPluginAction
{
    IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats { get; }
}

public static class FilePluginAction
{
    /// <inheritdoc cref="AssertListValid(IReadOnlyTaskFileListList?, string?)"/>
    public static void AssertListValid([NotNull] this IEnumerable<FileWithFormat>? files, [CallerArgumentExpression(nameof(files))] string? type = null)
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

    public static void ValidateInput(this TaskFileInput input, IReadOnlyCollection<IReadOnlyCollection<FileFormat>> formats)
    {
        input.Files.AssertListValid("input");

        TaskRequirement.EnsureFormats(input.Files, "input", formats)
            .Next(() => OperationResult.WrapException(() => input.Files.ValidateFileList("input")))
            .ThrowIfError(message => new TaskValidationException(message));
    }
}
public abstract class FilePluginAction<TData> : GPluginAction<TaskFileInput, TaskFileOutput, TData>
{
    public abstract IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats { get; }
    protected abstract OperationResult ValidateOutputFiles(TaskFilesCheckData files, TData data);

    protected sealed override void ValidateInput(TaskFileInput input, TData data) =>
        FilePluginAction.ValidateInput(input, InputFileFormats);

    protected sealed override void ValidateOutput(TaskFileInput input, TData data, TaskFileOutput output)
    {
        input.Files.AssertListValid("output");

        if (output.Files.Count == 0)
            validate(new TaskFilesCheckData(input.Files, new ReadOnlyTaskFileList(Enumerable.Empty<FileWithFormat>())));
        else
            foreach (var outfiles in output)
                validate(new TaskFilesCheckData(input.Files, outfiles));


        void validate(TaskFilesCheckData input)
        {
            ValidateOutputFiles(input, data)
                .Next(() => OperationResult.WrapException(() => input.OutputFiles.ValidateFileList("output")))
                .ThrowIfError(message => new TaskFailedException($"Output file validation failed: {message}"));
        }
    }
}
