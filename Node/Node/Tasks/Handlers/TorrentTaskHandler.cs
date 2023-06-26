using MonoTorrent;
using MonoTorrent.Client;

namespace Node.Tasks.Handlers;

public class TorrentTaskHandler : ITaskInputHandler, ITaskOutputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.Torrent;
    TaskOutputType ITaskOutputHandler.Type => TaskOutputType.Torrent;

    static TorrentClient TorrentClient => TorrentClientInstance.Instance;
    readonly Dictionary<string, TorrentManager> InputTorrents = new();

    public async ValueTask<ReadOnlyTaskFileList> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (TorrentTaskInputInfo) task.Input;
        if (task.IsFromSameNode())
            return new ReadOnlyTaskFileList(FileWithFormat.FromLocalPath(info.Path));


        info.Link.ThrowIfNull();
        var dir = task.FSInputDirectory();
        var manager = await TorrentClient.StartMagnet(MagnetLink.FromUri(new Uri(info.Link)), dir);

        await TorrentClient.AddTrackers(manager, true);
        await TorrentClient.WaitForCompletion(manager, new(cancellationToken, TimeSpan.FromMinutes(5)));
        return new ReadOnlyTaskFileList(FileWithFormat.FromLocalPath(dir));
    }

    public async ValueTask UploadResult(ReceivedTask task, ReadOnlyTaskFileList files, CancellationToken cancellationToken)
    {
        // TODO: fix uploading FSOutputDirectory instead of outputfiles

        if (task.IsFromSameNode())
        {
            CommonExtensions.CopyDirectory(task.FSOutputDirectory(), task.FSPlacedResultsDirectory());
            return;
        }

        var info = (TorrentTaskOutputInfo) task.Output;

        var (_, manager) = await TorrentClient.CreateAddTorrent(task.FSOutputDirectory());
        await TorrentClient.AddTrackers(manager, true);
        task.LogInfo($"Result magnet uri: {manager.MagnetLink.ToV1String()}");

        var post = await task.ShardGet("inittorrenttaskoutput", "Updating output magnet uri", ("taskid", task.Id), ("link", manager.MagnetLink.ToV1String()));
        post.ThrowIfError();

        info.Link = manager.MagnetLink.ToV1String();
        NodeSettings.QueuedTasks.Save(task);

        task.LogInfo($"Waiting for torrent result upload ({manager.InfoHash.ToHex()})");
        while (true)
        {
            await Task.Delay(30_000);

            var state = await task.GetTaskStateAsync().ThrowIfError();
            if (state is null || state.State.IsFinished()) return;
        }
    }

    public async ValueTask UploadInputFiles(DbTaskFullState task)
    {
        task.LogInfo("Starting torrent upload");

        var input = (TorrentTaskInputInfo) task.Input;
        var (_, manager) = await TorrentClient.CreateAddTorrent(input.Path);
        await TorrentClient.AddTrackers(manager, true);

        InputTorrents.Add(task.Id, manager);


        // if this node is behind nat, other nodes can't connect to this one, so this node should connect to others; for that we need to scrape
        // but automatic scrape happens every ~10min, too long
        new Thread(async () =>
        {
            while (InputTorrents.ContainsKey(task.Id))
            {
                await Task.Delay(20_000);
                await manager.TrackerManager.ScrapeAsync(CancellationToken.None);
            }
        })
        { IsBackground = true }.Start();
    }

    public async ValueTask OnPlacedTaskCompleted(DbTaskFullState task)
    {
        // if task is local, downloading already handled by UploadResult
        if (task.IsFromSameNode()) return;

        var output = (TorrentTaskOutputInfo) task.Output;
        output.Link.ThrowIfNull();
        task.LogInfo($"Downloading result from torrent {output.Link}");

        var manager = await TorrentClient.StartMagnet(output.Link, task.FSPlacedResultsDirectory());

        await TorrentClient.AddTrackers(manager, true);
        await TorrentClient.WaitForCompletion(manager, TimeSpan.FromMinutes(5));


        if (InputTorrents.Remove(task.Id, out var managerup))
            await managerup.StopAsync(TimeSpan.FromSeconds(5));
    }

    public ValueTask<bool> CheckCompletion(DbTaskFullState task) => ValueTask.FromResult(((TorrentTaskOutputInfo) task.Output).Link is not null);

    public ValueTask<OperationResult<TaskObject>> GetTaskObject(ITaskInputInfo input) => ((ILocalTaskInputInfo) input).GetTaskObject();
}