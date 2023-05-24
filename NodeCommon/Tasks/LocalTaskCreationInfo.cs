namespace NodeCommon.Tasks;

public record LocalTaskCreationInfo(JObject Data, ReadOnlyTaskFileList Input, TaskFileListList Output);