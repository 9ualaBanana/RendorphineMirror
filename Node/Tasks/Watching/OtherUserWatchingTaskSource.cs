using System.Web;
using Newtonsoft.Json.Linq;
using Node.Listeners;
using Node.P2P.Download;
using Node.P2P.Upload;

namespace Node.Tasks.Watching;

public class OtherUserWatchingTaskSource : IWatchingTaskSource
{
    public event Action<WatchingTaskFileAddedEventArgs>? FileAdded;

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

        var url = $"http://{node.Info.Ip}:{node.Info.Port}/dirdiff?path={HttpUtility.UrlEncode(Directory)}";

        while (true)
        {
            await Task.Delay(10_000);

            var check = await LocalApi.Send<DirectoryDiffListener.DiffOutput>(url + $"&lastcheck={LastCheck}");
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
                task.LogInfo($"Downloading {file}");

                var download = await LocalApi.Send(
                    $"download?url={HttpUtility.UrlEncode($"{await PortForwarding.GetPublicIPAsync()}:{PortForwarding.Port}")}&path={HttpUtility.UrlEncode(file)}");

                // TODO: get file, use it, upload back
            }
        }

        var pipe = await LocalPipe.SendAsync($"http://{node.Info.Ip}:{node.Info.Port}/watcher/listen?dir={HttpUtility.UrlEncode(Directory)}&sessionid={Settings.SessionId}").ConfigureAwait(false);
        var reader = LocalPipe.CreateReader(pipe);

        while (true)
        {
            var read = await reader.ReadAsync().ConfigureAwait(false);
            if (!read) break;

            var file = JToken.Load(reader).Value<NodeFileInfo>()!;

            var downloader = new PacketsDownloader(new P2P.Models.DownloadFileInfo(Settings.SessionId!, file.FileName, file.Size, Path.GetExtension(file.FileName).Substring(1)));
            downloader.DownloadCompleted += (_, path) => FileAdded?.Invoke(new(path, new UserTaskInputInfo(path)));

            await downloader.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
