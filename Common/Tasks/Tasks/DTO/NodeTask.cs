using System.IO.Compression;
using Common.Tasks.Models;

namespace Common.Tasks.Tasks.DTO;

public static class NodeTask
{
    public static string ZipFiles(IEnumerable<string> files)
    {
        var directoryName = Path.Combine(Path.GetTempPath(), "renderphine_temp");
        Directory.CreateDirectory(directoryName);
        var archiveName = Path.Combine(directoryName, Guid.NewGuid().ToString() + ".zip");


        using var archivefile = File.OpenWrite(archiveName);
        using var archive = new ZipArchive(archivefile, ZipArchiveMode.Create);

        foreach (var file in files)
            archive.CreateEntryFromFile(file, Path.GetFileName(file));

        return archiveName;
    }
    public static IEnumerable<string> UnzipFiles(string zipfile)
    {
        var directoryName = Path.Combine(Path.GetTempPath(), "renderphine_temp", Guid.NewGuid().ToString());
        Directory.CreateDirectory(directoryName);

        using var archivefile = File.OpenRead(zipfile);
        using var archive = new ZipArchive(archivefile, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            var path = Path.Combine(directoryName, entry.FullName);
            entry.ExtractToFile(path, true);

            yield return path;
        }
    }
}
public record NodeTask<T>(
    T Data,
    TaskObject Object,
    TaskInfo Input,
    TaskInfo Output) where T : IPluginActionData;
