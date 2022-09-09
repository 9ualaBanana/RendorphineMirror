using System.IO.Compression;
using System.Web;
using Node.Listeners;

namespace Node.Tasks.Watching;

public class OtherUserWatchingTaskSource : IWatchingTaskSource
{
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.OtherNodeTorrent;

    public readonly string NodeId, Directory;
    [DescriberIgnore] public long LastCheck = 0;

    public OtherUserWatchingTaskSource(string nodeid, string directory)
    {
        NodeId = nodeid;
        Directory = directory;
    }

    public void StartListening(WatchingTask task) => Start(task).Consume();
    async Task Start(WatchingTask task)
    {
        var node = (await Apis.GetNodeAsync(NodeId)).ThrowIfError();
        var url = $"http://{node.Info.Ip}:{node.Info.Port}";
        var path = $"dirdiff?sessionid={Settings.SessionId}&path={HttpUtility.UrlEncode(Directory)}";

        while (true)
        {
            try
            {
                await Task.Delay(60_000);

                if (task.PlacedTasks.Count != 0)
                    foreach (var taskid in task.PlacedTasks.ToArray())
                    {
                        var state = (await Apis.GetTaskStateAsync(taskid)).ThrowIfError();
                        if (!state.State.IsFinished()) continue;

                        if (state.State == TaskState.Finished)
                        {
                            var zipfile = Path.GetTempFileName();

                            try
                            {
                                var taskdir = ReceivedTask.FSResultsDirectory(taskid);
                                ZipFile.CreateFromDirectory(taskdir, zipfile);

                                using var stream = File.OpenRead(zipfile);
                                using var content = new StreamContent(stream);

                                (await Api.ApiPost($"{url}/download/uploadtask?sessionid={Settings.SessionId}&taskid={taskid}", "Uploading task result", content)).ThrowIfError();
                            }
                            finally { File.Delete(zipfile); }
                        }

                        task.LogInfo($"Placed task {taskid} was {state.State}, removing");
                        task.PlacedTasks.Remove(taskid);
                        NodeSettings.WatchingTasks.Save();
                    }


                var check = await LocalApi.Send<DirectoryDiffListener.DiffOutput>(url, path + $"&lastcheck={LastCheck}");
                check.LogIfError();
                if (!check) continue;

                var files = check.Value.Files;
                if (files.Length == 0) continue;
                files = files.Where(x => x.ModifTime > LastCheck).OrderBy(x => x.ModifTime).ToImmutableArray();

                task.LogInfo($"Found {files.Length} new files: {string.Join("; ", files)}");

                foreach (var file in files)
                {
                    var download = await Api.Download($"{url}/download?sessionid={Settings.SessionId}&path={HttpUtility.UrlEncode(file.Path)}");

                    var fsfile = Path.Combine(task.FSDataDirectory(), Path.GetFileName(file.Path));
                    using (var writer = File.OpenWrite(fsfile))
                        await download.CopyToAsync(writer);

                    await task.RegisterTask(fsfile, new TorrentTaskInputInfo(fsfile));
                    LastCheck = file.ModifTime;
                    NodeSettings.WatchingTasks.Save();
                }
            }
            catch (Exception ex) { task.LogErr(ex); }
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
