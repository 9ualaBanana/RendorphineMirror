namespace Node.Tasks;

public static class TaskRequirement
{
    /// <summary> Ensure input and output files have the same formats </summary>
    public static OperationResult EnsureSameFormats(ReceivedTask task) => EnsureOutputFormats(task, task.InputFiles.Select(f => f.Format).ToArray());
    public static OperationResult EnsureSameFormat(FileWithFormat output, FileWithFormat input) => EnsureFormat(output, input.Format);

    /// <summary> Ensure file format is either one from the expected array </summary>
    public static OperationResult EnsureFormat(FileWithFormat file, params FileFormat[] expected)
    {
        if (!expected.Contains(file.Format))
            return OperationResult.Err($"Invalid file format: {file.Format}; expected {string.Join(" or ", expected)}");

        return true;
    }

    public static OperationResult EnsureInputFormats(ReceivedTask task, IEnumerable<IReadOnlyCollection<FileFormat>> formats) => EnsureFormats(task.InputFiles, "input", formats);
    public static OperationResult EnsureOutputFormats(ReceivedTask task, IEnumerable<IReadOnlyCollection<FileFormat>> formats) => EnsureFormats(task.OutputFiles, "output", formats);
    public static OperationResult EnsureInputFormats(ReceivedTask task, params FileFormat[][] formats) => EnsureFormats(task.InputFiles, "input", formats);
    public static OperationResult EnsureOutputFormats(ReceivedTask task, params FileFormat[][] formats) => EnsureFormats(task.OutputFiles, "output", formats);
    static OperationResult EnsureFormats(HashSet<FileWithFormat> files, string info, IEnumerable<IReadOnlyCollection<FileFormat>> formats)
    {
        foreach (var format in formats.OrderByDescending(f => f.Count))
            if (check(format.ToList())) //.ToList() to create a copy
                return true;

        return ErrorFormats(files, info);


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
    public static OperationResult ErrorInputFormats(ReceivedTask task) => ErrorFormats(task.InputFiles, "input");
    public static OperationResult ErrorOutputFormats(ReceivedTask task) => ErrorFormats(task.OutputFiles, "output");
    static OperationResult ErrorFormats(HashSet<FileWithFormat> files, string info) =>
        OperationResult.Err($"Invalid {info} file formats ({string.Join(", ", files.Select(f => f.Format))})");

    public static OperationResult<FileWithFormat> EnsureSingleInputFile(ReceivedTask task) => EnsureInputFileCount(task, 1);
    public static OperationResult<FileWithFormat> EnsureSingleOutputFile(ReceivedTask task) => EnsureOutputFileCount(task, 1);
    public static OperationResult<FileWithFormat> EnsureInputFileCount(ReceivedTask task, int count) => EnsureInputFileCount(task, count, count);
    public static OperationResult<FileWithFormat> EnsureOutputFileCount(ReceivedTask task, int count) => EnsureOutputFileCount(task, count, count);
    public static OperationResult<FileWithFormat> EnsureInputFileCount(ReceivedTask task, int min, int max) => EnsureFileCount(task.InputFiles, "input", min, max);
    public static OperationResult<FileWithFormat> EnsureOutputFileCount(ReceivedTask task, int min, int max) => EnsureFileCount(task.OutputFiles, "output", min, max);

    static OperationResult<FileWithFormat> EnsureFileCount(HashSet<FileWithFormat> files, string info, int min, int max)
    {
        if (files.Count < min)
            return OperationResult.Err($"Not enough {info} files: {files.Count}; should be at least {min}");
        if (files.Count > max)
            return OperationResult.Err($"Too many {info} files: {files.Count}; should be no more than {max}");

        return files.First();
    }
}
