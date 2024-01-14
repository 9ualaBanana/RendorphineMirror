namespace NodeToUI;

public class OneClickTaskInfo
{
    public required string InputDir { get; init; }
    public required string OutputDir { get; init; }
    public required string LogDir { get; init; }
    public required string UnityTemplatesDir { get; init; }
    public required bool IsPaused { get; init; }
    public required IReadOnlyDictionary<string, ProjectExportInfo> ExportInfo { get; set; }
}