using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

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


    static string Added(this ReceivedTask task, ICollection<FileWithFormat> dict, FileFormat format, string path)
    {
        dict.Add(new(format, path));
        NodeSettings.QueuedTasks.Save(task);

        return path;
    }
    public static string FSNewInputFile(this ReceivedTask task, FileFormat format, string? path = null) =>
        task.Added(task.InputFiles, format, Path.Combine(task.FSInputDirectory(), path ?? ("input" + AsExtension(format))));
    public static string FSNewOutputFile(this ReceivedTask task, FileFormat format, string? path = null) =>
        task.Added(task.OutputFiles, format, Path.Combine(task.FSOutputDirectory(), path ?? ("output" + AsExtension(format))));

    public static void AddInputFromLocalPath(this ReceivedTask task, string path) => task.AddFromLocalPath(task.InputFiles, path);
    public static void AddOutputFromLocalPath(this ReceivedTask task, string path) => task.AddFromLocalPath(task.OutputFiles, path);
    static void AddFromLocalPath(this ReceivedTask task, ICollection<FileWithFormat> files, string path)
    {
        if (Directory.Exists(path)) addDir(path);
        else addFile(path);

        NodeSettings.QueuedTasks.Save(task);


        void addDir(string dir)
        {
            foreach (var dir2 in Directory.GetDirectories(dir))
                addDir(dir2);

            foreach (var file in Directory.GetFiles(dir))
                addFile(file);
        }
        void addFile(string file) => files.Add(new(FileFormatExtensions.FromFilename(file), file));
    }


    public static bool IsFromSameNode(this TaskBase task) => NodeSettings.QueuedTasks.ContainsKey(task.Id) && NodeSettings.PlacedTasks.ContainsKey(task.Id);
}
