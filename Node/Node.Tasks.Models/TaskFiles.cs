namespace Node.Tasks.Models;

public interface IReadOnlyTaskFiles
{
    ReadOnlyTaskFileList InputFiles { get; }
    IReadOnlyTaskFileListList OutputFiles { get; }
}
public record TaskFiles(ReadOnlyTaskFileList InputFiles, TaskFileListList OutputFiles) : IReadOnlyTaskFiles
{
    IReadOnlyTaskFileListList IReadOnlyTaskFiles.OutputFiles => OutputFiles;
}