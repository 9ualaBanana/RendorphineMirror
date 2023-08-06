using MonoTorrent;

namespace Node.Tasks.IO.GHandlers.Input;

public class TorrentTaskHandlerInfo : FileTaskInputHandlerInfo<TorrentTaskInputInfo>
{
    public override TaskInputType Type => TaskInputType.Torrent;
    protected override Type HandlerType => typeof(Handler);
    protected override Type TaskObjectProviderType => typeof(TaskObjectProvider);
    protected override Type InputUploaderType => typeof(InputUploader);


    class Handler : FileHandlerBase
    {
        public required TorrentClient TorrentClient { get; init; }
        public required IRegisteredTaskApi ApiTask { get; init; }

        public override async Task<ReadOnlyTaskFileList> Download(TorrentTaskInputInfo input, TaskObject obj, CancellationToken token)
        {
            if (ApiTask.IsFromSameNode())
                return new ReadOnlyTaskFileList(FileWithFormat.FromLocalPath(input.Path));

            input.Link.ThrowIfNull();
            var dir = TaskDirectoryProvider.InputDirectory;
            var manager = await TorrentClient.StartMagnet(MagnetLink.FromUri(new Uri(input.Link)), dir);

            await TorrentClient.AddTrackers(manager, true);
            await TorrentClient.WaitForCompletion(manager, new TimeoutCancellationToken(token, TimeSpan.FromMinutes(5)));
            return new ReadOnlyTaskFileList(FileWithFormat.FromLocalPath(dir));
        }
    }
    class TaskObjectProvider : TaskObjectProviderBase
    {
        public override Task<OperationResult<TaskObject>> GetTaskObject(TorrentTaskInputInfo input, CancellationToken token) =>
            GetLocalFileTaskObject(input.Path).AsOpResult().AsTask();
    }
    class InputUploader : InputUploaderBase
    {
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required TorrentClient TorrentClient { get; init; }

        public override async Task UploadInputFiles(TorrentTaskInputInfo input)
        {
            Logger.LogInformation("Starting torrent upload");

            var (_, manager) = await TorrentClient.CreateAddTorrent(input.Path);
            await TorrentClient.AddTrackers(manager, true);

            manager.PeerDisconnected += async (obj, e) =>
            {
                if (e.Peer.IsSeeder || e.Peer.PiecesReceived >= e.TorrentManager.PieceLength)
                    await TorrentClient.Client.RemoveAsync(manager);
            };

            // if this node is behind nat, other nodes can't connect to this one, so this node should connect to others; for that we need to scrape
            // but automatic scrape happens every ~10min, too long
            new Thread(async () =>
            {
                while (TorrentClient.Client.Torrents.Contains(manager) && manager.Peers.Leechs == 0)
                {
                    await Task.Delay(20_000);
                    await manager.TrackerManager.ScrapeAsync(CancellationToken.None);
                }
            })
            { IsBackground = true }.Start();
        }
    }
}
