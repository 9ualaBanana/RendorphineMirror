namespace Node.Tasks;

public record TaskFilesCheckData(IReadOnlyTaskFileList InputFiles, IReadOnlyTaskFileList OutputFiles);