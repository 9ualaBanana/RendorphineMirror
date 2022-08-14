using System.IO.Compression;
using Newtonsoft.Json;

namespace Node.Tasks.Models;

public static class NodeTask
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static async ValueTask<OperationResult<string>> RegisterOrExecute(TaskCreationInfo info)
    {
        OperationResult<string> taskid;
        if (info.ExecuteLocally)
        {
            taskid = ReceivedTask.GenerateLocal();

            // TODO: fill in TaskObject
            var tk = new ReceivedTask(taskid.Value, new TaskInfo(new("file.mov", 123), info.Input, info.Output, info.Data), true);
            TaskHandler.HandleAsync(tk, new(), default).Consume();
        }
        else taskid = await RegisterAsync(info).ConfigureAwait(false);

        return taskid;
    }

    public static async ValueTask<OperationResult<string>> RegisterAsync(TaskCreationInfo info)
    {
        var data = info.Data;
        var taskobj = new TaskObject("3_UGVlayAyMDIxLTA4LTA0IDEzLTI5", 12345678);
        var input = info.Input;
        var output = info.Output;

        var values = new List<(string, string)>()
        {
            ("sessionid", Settings.SessionId!),
            ("object", JsonConvert.SerializeObject(taskobj, JsonSettings.LowercaseIgnoreNull)),
            ("input", input.ToString(Formatting.None)),
            ("output", output.ToString(Formatting.None)),
            ("data", data.ToString(Formatting.None)),
            ("origin", string.Empty),
        };
        if (info.Version is not null)
        {
            var soft = new[] { new TaskSoftwareRequirement(info.Type.ToString().ToLowerInvariant(), ImmutableArray.Create(info.Version), null), };
            values.Add(("software", JsonConvert.SerializeObject(soft, JsonSettings.LowercaseIgnoreNull)));
        }

        _logger.Info("Registering task: {Task}", JsonConvert.SerializeObject(info));
        var idr = await Api.ApiPost<string>($"{Api.TaskManagerEndpoint}/registermytask", "taskid", "Registering task", values.ToArray());
        var id = idr.ThrowIfError();

        _logger.Info("Task registered with ID {Id}", id);
        NodeSettings.PlacedTasks.Bindable.Add(new(id, info));
        return id;
    }

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