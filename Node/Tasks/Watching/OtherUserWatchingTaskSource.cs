using System.Web;
using Newtonsoft.Json;
using Node.Listeners;

namespace Node.Tasks.Watching;

public class OtherUserWatchingTaskSource : IWatchingTaskSource
{
    public event Action<WatchingTaskFileAddedEventArgs>? FileAdded;
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.OtherNodeTorrent;

    public readonly string NodeId, Directory;
    public long LastCheck = 0;

    public OtherUserWatchingTaskSource(string nodeid, string directory)
    {
        NodeId = nodeid;
        Directory = directory;
    }

    public void StartListening(WatchingTask task) => Start(task).Consume();
    async Task Start(WatchingTask task)
    {
        var nodesr = await Apis.GetMyNodesAsync().ConfigureAwait(false);
        var nodes = nodesr.ThrowIfError();
        var node = nodes.FirstOrDefault(x => x.Id == NodeId);
        if (node is null) throw new Exception($"Could not find local node with ID {NodeId}");

        var url = $"http://{node.Info.Ip}:{node.Info.Port}";
        var path = $"dirdiff?sessionid={Settings.SessionId}&path={HttpUtility.UrlEncode(Directory)}";

        while (true)
        {
            try
            {
                await Task.Delay(10_000);

                var check = await LocalApi.Send<DirectoryDiffListener.DiffOutput>(url, path + $"&lastcheck={LastCheck}");
                check.LogIfError();
                task.LogInfo("download " + check.Success);
                if (!check) continue;

                if (LastCheck != check.Value.ModifTime)
                {
                    LastCheck = check.Value.ModifTime;
                    NodeSettings.WatchingTasks.Save();
                }

                var files = check.Value.Files;
                if (files.Length == 0) continue;

                task.LogInfo($"Found {files.Length} new files: {string.Join("; ", files)}");

                foreach (var file in files)
                {
                    var info = task.CreateTaskInfo(new TorrentTaskInputInfo(Path.Combine(Directory, file)), task.Output.CreateOutput(file));
                    var taskid = await LocalApi.Post<string>(url, "tasks/start", new StringContent(JsonConvert.SerializeObject(info, JsonSettings.LowercaseIgnoreNull)));

                    task.LogInfo($"Placed remote task {taskid} on node {node.Id} {node.Info.Nickname}");
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
