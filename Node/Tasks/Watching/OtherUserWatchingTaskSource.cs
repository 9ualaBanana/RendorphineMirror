using System.Web;
using Newtonsoft.Json.Linq;
using Node.Listeners;
using Node.P2P.Download;

namespace Node.Tasks.Watching;

public class OtherUserWatchingTaskSource : IWatchingTaskSource
{
    public event Action<WatchingTaskFileAddedEventArgs>? FileAdded;

    public readonly string NodeId, Directory;

    public OtherUserWatchingTaskSource(string nodeid, string directory)
    {
        NodeId = nodeid;
        Directory = directory;
    }

    public void StartListening() => Start().Consume();
    async Task Start()
    {
        var nodesr = await Apis.GetMyNodesAsync().ConfigureAwait(false);
        var nodes = nodesr.ThrowIfError();
        var node = nodes.FirstOrDefault(x => x.Id == NodeId);
        if (node is null) throw new Exception($"Could not find local node with ID {NodeId}");

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
