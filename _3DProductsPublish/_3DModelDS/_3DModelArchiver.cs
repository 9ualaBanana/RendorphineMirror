using System.IO.Compression;

namespace _3DProductsPublish._3DModelDS;

internal static class _3DModelArchiver
{
    internal static async Task<string> ArchiveAsync(_3DModel _3DModel, CancellationToken cancellationToken) =>
        await Task.Run(() => Archive(_3DModel), cancellationToken);

    internal static string Archive(_3DModel _3DModel)
    {
        DirectoryInfo tempDirectoryToArchive = CreateTempDirectoryThatWillBeArchived();
        _3DModel.Files.CopyTo(tempDirectoryToArchive);
        var archivePath = Path.ChangeExtension(tempDirectoryToArchive.FullName, ".zip");
        tempDirectoryToArchive.DeleteAfter(
            () => ZipFile.CreateFromDirectory(tempDirectoryToArchive.FullName, archivePath)
            );

        return archivePath;
    }

    static DirectoryInfo CreateTempDirectoryThatWillBeArchived()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var tempDirectory = new DirectoryInfo(tempDirectoryPath);
        tempDirectory.Create();
        return tempDirectory;
    }
}

static class _3DModelFilesExtensions
{
    internal static void CopyTo(this IEnumerable<string> files, DirectoryInfo directory)
    {
        foreach (string filePath in files)
        {
            string _3DModelFilePathInsideDestinationDirectory = Path.Combine(
                directory.FullName, Path.GetFileName(filePath)
                );
            File.Copy(filePath, _3DModelFilePathInsideDestinationDirectory);
        }
    }
}
