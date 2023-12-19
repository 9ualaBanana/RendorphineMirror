namespace Node.Tasks.Watching.Handlers.Input;

public class OneClickRunnerInfo
{
    /// <summary> C:\oneclick\input </summary>
    public string InputDir => Test ? Input.TestInputDirectory : Input.InputDirectory;

    /// <summary> C:\oneclick\output </summary>
    public string OutputDir => Test ? Input.TestOutputDirectory : Input.OutputDirectory;

    /// <summary> C:\oneclick\log </summary>
    public string LogDir => Test ? Input.TestLogDirectory : Input.LogDirectory;

    /// <summary> C:\OneClickUnityDefaultProjects\ </summary>
    public string UnityTemplatesDir { get; init; } = @"C:\\OneClickUnityDefaultProjects";

    public OneClickWatchingTaskInputInfo Input { get; }
    readonly bool Test;

    public OneClickRunnerInfo(OneClickWatchingTaskInputInfo input, bool test = false)
    {
        Input = input;
        Test = test;
    }

    /// <summary> C:\oneclick\output\{SmallGallery} </summary>
    public string Achive3dsMaxExtractDirectory(string archiveFilePath) => Path.Combine(OutputDir, Path.GetFileNameWithoutExtension(archiveFilePath));

    /// <summary> C:\oneclick\output\{SmallGallery} [MaxOcExport] </summary>
    public string Export3dsMaxResultDirectory(string archiveFilePath) => Path.Combine(OutputDir, Path.GetFileNameWithoutExtension(archiveFilePath) + " [MaxOcExport]");

    public ImmutableArray<ProjectExportInfo> GetExportInfosByArchiveFiles(IReadOnlyList<string> inputArchiveFiles)
    {
        return inputArchiveFiles
            .Select(Achive3dsMaxExtractDirectory)
            .Where(Directory.Exists)
            .Select(GetMaxSceneFile)
            .Select(Path.GetFileNameWithoutExtension)
            .Select(GetExportInfoByProductName!)
            .ToImmutableArray();
    }
    public static string GetMaxSceneFile(string dir)
    {
        var maxSceneFile = Directory.GetFiles(dir, "*.max", SearchOption.AllDirectories)
              .Where(zip => !zip.ContainsOrdinal("backup"))
              .MaxBy(File.GetLastWriteTimeUtc);
        maxSceneFile ??= Directory.GetFiles(dir, "*.max", SearchOption.AllDirectories)
            .MaxBy(File.GetLastWriteTimeUtc);

        return maxSceneFile.ThrowIfNull("No .max file found");
    }

    public ProjectExportInfo GetExportInfoByProductName(string productName)
    {
        return (Input.ExportInfo ??= []).TryGetValue(productName, out var exportInfo)
             ? exportInfo
             : Input.ExportInfo[productName] = new() { ProductName = productName };
    }
}
