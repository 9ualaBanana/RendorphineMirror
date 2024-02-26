namespace NodeToUI;

public record AutoRFProductPublishInfo
{
    public required bool IsPaused { get; init; }
    public required string InputDirectory { get; init; }
    public required string RFProductDirectory { get; init; }

    public string? CurrentRFProducting { get; init; }
    public string? CurrentPublishing { get; init; }

    public int? RFProductedCount { get; init; }
    public int? PublishedCount { get; init; }
    public int FileCount { get; init; }
}
