namespace NodeCommon.Tasks.Watching;

public class OneClickWatchingTaskInputInfo : IWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.OneClick;

    [LocalDirectory]
    public string InputDirectory { get; }

    [LocalDirectory]
    public string OutputDirectory { get; }

    [LocalDirectory]
    public string ResultDirectory { get; }

    [LocalDirectory]
    public string LogDirectory { get; }

    [LocalDirectory]
    public string TestMzpDirectory { get; }

    [LocalDirectory]
    public string TestInputDirectory { get; }

    [LocalDirectory]
    public string TestOutputDirectory { get; }

    [LocalDirectory]
    public string TestResultDirectory { get; }

    [LocalDirectory]
    public string TestLogDirectory { get; }

    [Hidden]
    public string? UnityProjectsCommitHash { get; set; }

    public OneClickWatchingTaskInputInfo(string inputDirectory, string outputDirectory, string resultDirectory, string logDirectory, string testMzpDirectory, string testInputDirectory, string testOutputDirectory, string testResultDirectory, string testLogDirectory)
    {
        InputDirectory = inputDirectory;
        OutputDirectory = outputDirectory;
        ResultDirectory = resultDirectory;
        LogDirectory = logDirectory;
        TestMzpDirectory = testMzpDirectory;
        TestInputDirectory = testInputDirectory;
        TestOutputDirectory = testOutputDirectory;
        TestResultDirectory = testResultDirectory;
        TestLogDirectory = testLogDirectory;
    }
}
