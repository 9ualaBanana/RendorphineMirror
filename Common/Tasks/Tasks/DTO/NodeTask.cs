using System.IO.Compression;

namespace Common.Tasks.Tasks.DTO;

public record NodeTask<T>(
    TaskData<T> Data,
    FileInfo File,
    TaskInfo Input,
    TaskInfo Output) where T : IPluginActionData
{
    public NodeTask(
        TaskData<T> data,
        IEnumerable<string> files,
        TaskInfo input,
        TaskInfo output) : this(data, ZipFiles(files), input, output)
    {
    }

    /// <exception cref="IOException"></exception>
    static FileInfo ZipFiles(IEnumerable<string> files)
    {
        var directoryName = Guid.NewGuid().ToString();
        var archiveName = Path.ChangeExtension(directoryName, ".zip");
        try
        {
            Directory.CreateDirectory(directoryName);
            foreach (var fileName in files) System.IO.File.Move(fileName, Path.Combine(directoryName, fileName));
            ZipFile.CreateFromDirectory(directoryName, archiveName);
            return new(archiveName);
        }
        catch (Exception ex)
        {
            throw new IOException("Archive for task files couldn't be created.", ex);
        }
    }
}
