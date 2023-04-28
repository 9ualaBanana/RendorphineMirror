namespace Node.Tasks;

public static class TaskRequirement
{
    /// <summary> Ensure input and output files have the same formats </summary>
    public static OperationResult EnsureSameFormats(this TaskFilesCheckData data) => EnsureOutputFormats(data, data.InputFiles.Select(f => f.Format).ToArray());
    public static OperationResult EnsureSameFormat(FileWithFormat output, FileWithFormat input) => EnsureFormat(output, input.Format);

    /// <summary> Ensure file format is either one from the expected array </summary>
    public static OperationResult EnsureFormat(FileWithFormat file, params FileFormat[] expected)
    {
        if (!expected.Contains(file.Format))
            return OperationResult.Err($"Invalid file format: {file.Format}; expected {string.Join(" or ", expected)}");

        return true;
    }

    public static OperationResult EnsureInputFormats(this TaskFilesCheckData files, IReadOnlyCollection<IReadOnlyCollection<FileFormat>> formats) => EnsureFormats(files.InputFiles, "input", formats);
    public static OperationResult EnsureOutputFormats(this TaskFilesCheckData files, IReadOnlyCollection<IReadOnlyCollection<FileFormat>> formats) => EnsureFormats(files.OutputFiles, "output", formats);
    public static OperationResult EnsureInputFormats(this TaskFilesCheckData files, params FileFormat[][] formats) => EnsureFormats(files.InputFiles, "input", formats);
    public static OperationResult EnsureOutputFormats(this TaskFilesCheckData files, params FileFormat[][] formats) => EnsureFormats(files.OutputFiles, "output", formats);
    public static OperationResult EnsureFormats(ReadOnlyTaskFileList files, string info, IReadOnlyCollection<IReadOnlyCollection<FileFormat>> expected)
    {
        foreach (var format in expected.OrderByDescending(f => f.Count))
            if (check(format.ToList())) //.ToList() to create a copy
                return true;

        return ErrorFormats(files, expected, info);


        bool check(List<FileFormat> formats)
        {
            foreach (var file in files.Select(f => f.Format))
            {
                if (!formats.Contains(file))
                    return false;

                formats.Remove(file);
            }

            return formats.Count == 0;
        }
    }
    public static OperationResult ErrorInputFormats(this TaskFilesCheckData task) => ErrorFormats(task.InputFiles, "input");
    public static OperationResult ErrorOutputFormats(this TaskFilesCheckData task) => ErrorFormats(task.OutputFiles, "output");
    static OperationResult ErrorFormats(ReadOnlyTaskFileList files, string info) =>
        OperationResult.Err($"Invalid {info} file formats ({string.Join(", ", files.Select(f => f.Format))})");
    static OperationResult ErrorFormats(ReadOnlyTaskFileList files, IEnumerable<IReadOnlyCollection<FileFormat>> expected, string info) =>
        OperationResult.Err($"{ErrorFormats(files, info).Message}; Expected: ({string.Join(", ", expected.Select(f => $"({string.Join(", ", f)})"))})");

    public static OperationResult<FileWithFormat> EnsureSingleInputFile(this TaskFilesCheckData task) => EnsureInputFileCount(task, 1);
    public static OperationResult<FileWithFormat> EnsureSingleOutputFile(this TaskFilesCheckData task) => EnsureOutputFileCount(task, 1);
    public static OperationResult<FileWithFormat> EnsureInputFileCount(this TaskFilesCheckData task, int count) => EnsureInputFileCount(task, count, count);
    public static OperationResult<FileWithFormat> EnsureOutputFileCount(this TaskFilesCheckData task, int count) => EnsureOutputFileCount(task, count, count);
    public static OperationResult<FileWithFormat> EnsureInputFileCount(this TaskFilesCheckData task, int min, int max) => EnsureFileCount(task.InputFiles, "input", min, max);
    public static OperationResult<FileWithFormat> EnsureOutputFileCount(this TaskFilesCheckData task, int min, int max) => EnsureFileCount(task.OutputFiles, "output", min, max);

    public static OperationResult<FileWithFormat> EnsureSingle(ReadOnlyTaskFileList files, string info) => EnsureFileCount(files, info, 1, 1);

    static OperationResult<FileWithFormat> EnsureFileCount(ReadOnlyTaskFileList files, string info, int min, int max)
    {
        if (files.Count < min)
            return OperationResult.Err($"Not enough {info} files: {files.Count}; should be at least {min}");
        if (files.Count > max)
            return OperationResult.Err($"Too many {info} files: {files.Count}; should be no more than {max}");

        return files.First();
    }
}
