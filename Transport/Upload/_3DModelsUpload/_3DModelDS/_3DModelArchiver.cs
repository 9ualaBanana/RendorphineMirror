using System.IO.Compression;

namespace Transport.Upload._3DModelsUpload._3DModelDS;

internal static class _3DModelArchiver
{
    internal static string _Archive(_3DModel _3DModel)
    {
        DirectoryInfo tempDirectoryToArchive = _CreateTempDirectoryThatWillBeArchived();
        _3DModel.Files._CopyTo(tempDirectoryToArchive);
        var archivePath = Path.ChangeExtension(tempDirectoryToArchive.FullName, ".zip");
        tempDirectoryToArchive.DeleteAfter(
            () => ZipFile.CreateFromDirectory(tempDirectoryToArchive.FullName, archivePath)
            );

        return archivePath;
    }

    static DirectoryInfo _CreateTempDirectoryThatWillBeArchived()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var tempDirectory = new DirectoryInfo(tempDirectoryPath);
        tempDirectory.Create();
        return tempDirectory;
    }
}

static class _3DModelFilesExtensions
{
    internal static void _CopyTo(this IEnumerable<string> files, DirectoryInfo directory)
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
