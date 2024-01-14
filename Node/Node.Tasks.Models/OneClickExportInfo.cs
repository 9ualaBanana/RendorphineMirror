namespace Node.Tasks.Models;

public record UnityBakedExportInfo(string ImporterVersion, string UnityVersion, string RendererType, string? LaunchArgs);

public record OneClickProjectExportInfo(string Version, bool Successful);
public record UnityProjectExportInfo(string ImporterVersion, string UnityVersion, string RendererType, string ImporterCommitHash, ProductJson? ProductInfo)
{
    public bool Successful => ProductInfo is not null;
}
public class ProjectExportInfo
{
    public required string ProductName { get; init; }
    public OneClickProjectExportInfo? OneClick { get; set; }
    public Dictionary<string, UnityProjectExportInfo>? Unity { get; set; }
}

public record ProductJson(
    // entrance_hall_for_export
    string OCPName,
    // _[2021.3.32f1]_[URP]_[85]
    string OCVersion,
    ImmutableArray<string> VideoPreviews,
    ImmutableArray<string> ImagePreviews
);

