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


    /*static string Added(this ReceivedTask task, ICollection<FileWithFormat> dict, FileFormat format, string path, string loginfo)
    {
        task.LogTrace($"New {loginfo} file: {format} {path}");
        dict.Add(new(format, path));
        NodeSettings.QueuedTasks.Save(task);

        return path;
    }
    public static string FSNewInputFile(this ReceivedTask task, FileFormat format, string? path = null) =>
        task.Added(task.InputFiles, format, Path.Combine(task.FSInputDirectory(), path ?? ("input" + AsExtension(format))), "input");
    public static string FSNewOutputFile(this ReceivedTask task, FileFormat format, string? path = null) =>
        task.Added(task.OutputFiles, format, Path.Combine(task.FSOutputDirectory(), path ?? ("output" + AsExtension(format))), "output");

    public static void AddInputFromLocalPath(this ReceivedTask task, string path) => task.AddFromLocalPath(task.InputFiles, path, "input");
    public static void AddOutputFromLocalPath(this ReceivedTask task, string path) => task.AddFromLocalPath(task.OutputFiles, path, "output");
    static void AddFromLocalPath(this ReceivedTask task, ICollection<FileWithFormat> files, string path, string loginfo)
    {
        task.LogTrace($"Adding {loginfo} files from {path}");
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
        void addFile(string file)
        {
            var format = FileFormatExtensions.FromFilename(file);
            task.LogTrace($"New {loginfo} file: {format} {file}");

            files.Add(new(FileFormatExtensions.FromFilename(file), file));
        }
    }*/


    public static bool IsFromSameNode(this TaskBase task) => NodeSettings.QueuedTasks.ContainsKey(task.Id) && NodeSettings.PlacedTasks.ContainsKey(task.Id);
    public static bool IsFromSameNode(this IRegisteredTask task) => NodeSettings.QueuedTasks.ContainsKey(task.Id) && NodeSettings.PlacedTasks.ContainsKey(task.Id);
    public static void Populate(this DbTaskFullState task, ITaskStateInfo info)
    {
        if (info is TMTaskStateInfo tsi) task.Populate(tsi);
        if (info is TMOldTaskStateInfo osi) task.Populate(osi);
        if (info is ServerTaskState sts) task.Populate(sts);
    }
    public static void Populate(this DbTaskFullState task, TMTaskStateInfo info) => task.Progress = info.Progress;
    public static void Populate(this DbTaskFullState task, TMOldTaskStateInfo info)
    {
        task.State = info.State;
        if (info.Output is not null)
            JsonSettings.Default.Populate(JObject.FromObject(info.Output).CreateReader(), task.Output);
    }
    public static void Populate(this DbTaskFullState task, ServerTaskState info)
    {
        task.State = info.State;
        task.Progress = info.Progress;
        task.Times = info.Times;
        // task.Server = info.Server;

        if (info.Output is not null)
            JsonSettings.Default.Populate(JObject.FromObject(info.Output).CreateReader(), task.Output);
    }
}
