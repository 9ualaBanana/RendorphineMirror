namespace NodeCommon.Tasks.Watching;

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

public class OneClickWatchingTaskInputInfo : IWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.OneClick;

    [LocalDirectory]
    public string InputDirectory { get; }

    [LocalDirectory]
    public string OutputDirectory { get; }

    [LocalDirectory]
    public string LogDirectory { get; }

    [LocalDirectory]
    public string TestMzpDirectory { get; }

    [LocalDirectory]
    public string TestInputDirectory { get; }

    [LocalDirectory]
    public string TestOutputDirectory { get; }

    [LocalDirectory]
    public string TestLogDirectory { get; }

    [Hidden]
    public string? UnityProjectsCommitHash { get; set; }

    [Hidden]
    public Dictionary<string, ProjectExportInfo>? ExportInfo { get; set; }

    public OneClickWatchingTaskInputInfo(string inputDirectory, string outputDirectory, string logDirectory, string testMzpDirectory, string testInputDirectory, string testOutputDirectory, string testLogDirectory, Dictionary<string, ProjectExportInfo>? exportInfo = null)
    {
        InputDirectory = inputDirectory;
        OutputDirectory = outputDirectory;
        LogDirectory = logDirectory;
        TestMzpDirectory = testMzpDirectory;
        TestInputDirectory = testInputDirectory;
        TestOutputDirectory = testOutputDirectory;
        TestLogDirectory = testLogDirectory;
        ExportInfo = exportInfo;
    }
}
