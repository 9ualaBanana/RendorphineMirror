using System.IO.Compression;

namespace _3DProductsPublish._3DProductDS;

internal static class _3DModelArchiver
{
    internal static async ValueTask<string> ArchiveAsync(_3DModel _3DModel, CancellationToken cancellationToken) =>
        await Task.Run(() => Archive(_3DModel), cancellationToken);
    internal static string Archive(_3DModel _3DModel)
    {
        if (_3DModel.OriginalContainer is _3DModel.ContainerType.Archive)
            return _3DModel.OriginalPath;
        else
        {
            DirectoryInfo archiveBuffer = TempDirectory();
            _3DModel.Files.CopyTo(archiveBuffer);
            var archivePath = Path.ChangeExtension(archiveBuffer.FullName, ".zip");
            archiveBuffer.DeleteAfter(
                () => ZipFile.CreateFromDirectory(archiveBuffer.FullName, archivePath)
                );

            return archivePath;
        }
    }

    internal static async ValueTask<string> UnpackAsync(_3DModel _3DModel, CancellationToken cancellationToken)
        => await Task.Run(() => Unpack(_3DModel), cancellationToken);
    internal static string Unpack(_3DModel _3DModel)
    {
        if (_3DModel.OriginalContainer is _3DModel.ContainerType.Directory)
            return _3DModel.OriginalPath;
        else
        {
            DirectoryInfo unpacked3DModel = TempDirectory();
            ZipFile.ExtractToDirectory(_3DModel.OriginalPath, unpacked3DModel.FullName);
            return unpacked3DModel.FullName;
        }
    }


    static DirectoryInfo TempDirectory()
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
            string destinationFilePath = Path.Combine(directory.FullName, Path.GetFileName(filePath));
            File.Copy(filePath, destinationFilePath);
        }
    }
}
