namespace NodeCommon.Tasks.Watching;

public class OneClickWatchingTaskInputInfo : IWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.OneClick;

    [LocalDirectory]
    public string InputDirectory { get; }

    [LocalDirectory]
    public string OutputDirectory { get; }

    [LocalDirectory]
    public string LogDirectory { get; }

    public bool AutoCreateRFProducts { get; init; } = false;

    [LocalDirectory]
    public string RFProductTargetDirectory { get; init; } = "";

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
    public int? Launched3dsMaxProcessId { get; set; }

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
