namespace NodeCommon.Tasks;

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
        return GetExportInfosByArchiveFilesDict(inputArchiveFiles).Values
            .ToImmutableArray();
    }
    // <string archiveFile, ProjectExportInfo exportInfo>
    public IReadOnlyDictionary<string, ProjectExportInfo> GetExportInfosByArchiveFilesDict(IReadOnlyList<string> inputArchiveFiles)
    {
        return inputArchiveFiles
            .Select(f => KeyValuePair.Create(Path.GetFileName(f), Achive3dsMaxExtractDirectory(f)))
            .Where(f => Directory.Exists(f.Value))
            .Select(f => KeyValuePair.Create(f.Key, GetMaxSceneFile(f.Value)))
            .Select(f => KeyValuePair.Create(f.Key, Path.GetFileNameWithoutExtension(f.Value)))
            .Select(f => KeyValuePair.Create(f.Key, GetExportInfoByProductName(f.Value)!))
            .ToImmutableDictionary();
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
