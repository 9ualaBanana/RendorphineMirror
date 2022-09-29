using System.Diagnostics.CodeAnalysis;

namespace Node.Tasks;

public static class ReceivedTaskExtensions
{
    [return: NotNullIfNotNull("extension")]
    static string? AsExtension(string? extension)
    {
        extension = Path.GetExtension(extension);
        if (extension is null || extension.StartsWith('.'))
            return extension;

        return "." + extension;
    }
    static string? AsExtension(FileFormat format) => AsExtension("." + format.ToString().ToLowerInvariant());


    static string Added(List<FileWithFormat> dict, FileFormat format, string path)
    {
        dict.Add(new(format, path));
        NodeSettings.QueuedTasks.Save();

        return path;
    }
    public static string FSNewInputFile(this ReceivedTask task, FileFormat format, string? path = null) =>
        Added(task.InputFiles, format, Path.Combine(task.FSInputDirectory(), path ?? ("input" + AsExtension(format))));
    public static string FSNewOutputFile(this ReceivedTask task, FileFormat format, string? path = null) =>
        Added(task.OutputFiles, format, Path.Combine(task.FSOutputDirectory(), path ?? ("output" + AsExtension(format))));

    public static void AddInputFromLocalPath(this ReceivedTask task, string path) => AddFromLocalPath(task.InputFiles, path);
    public static void AddOutputFromLocalPath(this ReceivedTask task, string path) => AddFromLocalPath(task.OutputFiles, path);
    static void AddFromLocalPath(List<FileWithFormat> files, string path)
    {
        if (Directory.Exists(path)) addDir(path);
        else addFile(path);

        NodeSettings.QueuedTasks.Save();


        void addDir(string dir)
        {
            foreach (var dir2 in Directory.GetDirectories(dir))
                addDir(dir2);

            foreach (var file in Directory.GetFiles(dir))
                addFile(file);
        }
        void addFile(string file) => files.Add(new(FileFormatExtensions.FromFilename(file), file));
    }
}
