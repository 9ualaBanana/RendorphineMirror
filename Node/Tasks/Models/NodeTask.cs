using System.IO.Compression;

namespace Node.Tasks.Models;

public static class NodeTask
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static async ValueTask<OperationResult<string>> RegisterOrExecute(TaskCreationInfo info)
    {
        OperationResult<string> taskid;
        if (info.ExecuteLocally)
        {
            taskid = ReceivedTask.GenerateLocalId();

            // TODO: fill in TaskObject
            var tk = new ReceivedTask(taskid.Value, new TaskInfo(new("file.mov", 123), info.Input, info.Output, info.Data), true);
            TaskHandler.HandleReceivedTask(tk).Consume();
        }
        else taskid = await TaskRegistration.RegisterAsync(info).ConfigureAwait(false);

        return taskid;
    }


    public static string ZipFiles(IEnumerable<string> files)
    {
        var directoryName = Path.Combine(Path.GetTempPath(), "renderphin_temp");
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
        var directoryName = Path.Combine(Path.GetTempPath(), "renderphin_temp", Guid.NewGuid().ToString());
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