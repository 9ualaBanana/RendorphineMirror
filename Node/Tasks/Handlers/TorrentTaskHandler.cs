using MonoTorrent;
using MonoTorrent.Client;

namespace Node.Tasks.Handlers;

public class TorrentTaskHandler : ITaskInputHandler, ITaskOutputHandler, IPlacedTaskInitializationHandler, IPlacedTaskOnCompletedHandler, IPlacedTaskCompletionCheckHandler, IPlacedTaskResultDownloadHandler
{
    public TaskInputOutputType Type => TaskInputOutputType.Torrent;

    readonly Dictionary<string, TorrentManager> InputTorrents = new();

    public async ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (TorrentTaskInputInfo) task.Input;
        info.Link.ThrowIfNull();

        if (task.ExecuteLocally || task.Info.LaunchPolicy == TaskPolicy.SameNode)
            return info.Path;


        var dir = task.FSInputDirectory();
        var manager = await TorrentClient.StartMagnet(MagnetLink.FromUri(new Uri(info.Link)), dir);

        await TorrentClient.AddTrackers(manager, true);
        await TorrentClient.WaitForCompletion(manager, cancellationToken);
        return Directory.GetFiles(dir).Single();
    }

    public async ValueTask UploadResult(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (TorrentTaskOutputInfo) task.Output;
        var outputdir = task.FSResultsDirectory();

        if (task.ExecuteLocally || task.Info.LaunchPolicy == TaskPolicy.SameNode)
        {
            copydir(task.FSOutputDirectory(), outputdir);
            return;


            void copydir(string source, string destination)
            {
                source = Path.GetFullPath(source);
                destination = Path.GetFullPath(destination);

                Directory.GetDirectories(source, "*", SearchOption.AllDirectories).AsParallel().ForAll(x => Directory.CreateDirectory(x.Replace(source, destination)));
                Directory.GetFiles(source, "*", SearchOption.AllDirectories).AsParallel().ForAll(x => File.Copy(x, x.Replace(source, destination)));
            }
        }


        var (_, manager) = await TorrentClient.CreateAddTorrent(task.FSOutputDirectory());
        await TorrentClient.AddTrackers(manager, true);
        task.LogInfo($"Result magnet uri: {manager.MagnetLink.ToV1String()}");

        var post = await Api.ApiPost($"{Api.TaskManagerEndpoint}/inittorrenttaskoutput", "Updating output magnet uri", ("sessionid", Settings.SessionId), ("taskid", task.Id), ("link", manager.MagnetLink.ToV1String()));
        post.ThrowIfError();

        info.Link = manager.MagnetLink.ToV1String();
        NodeSettings.QueuedTasks.Save();

        task.LogInfo($"Waiting for torrent result upload ({manager.InfoHash.ToHex()})");
        while (true)
        {
            await Task.Delay(30_000);

            var state = (await task.GetTaskStateAsync()).ThrowIfError();
            if (state.State.IsFinished()) return;
        }
    }
    public async ValueTask DownloadResult(DbTaskFullState task)
    {
        // if task is local, downloading already handled by UploadResult
        if (task.ExecuteLocally || task.Info.LaunchPolicy == TaskPolicy.SameNode)
            return;

        var output = (TorrentTaskOutputInfo) task.Output;
        output.Link.ThrowIfNull();
        task.LogInfo($"Downloading result from torrent {output.Link}");

        var manager = await TorrentClient.StartMagnet(output.Link, task.FSResultsDirectory());

        await TorrentClient.AddTrackers(manager, true);
        await TorrentClient.WaitForCompletion(manager);
    }

    public async ValueTask InitializePlacedTaskAsync(DbTaskFullState task)
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
        if (InputTorrents.Remove(task.Id, out var managerup))
            await managerup.StopAsync(TimeSpan.FromSeconds(5));
    }

    public async ValueTask<bool> CheckCompletion(DbTaskFullState task)
    {
        var output = (TorrentTaskOutputInfo) task.Output;
        var state = (await Apis.GetTaskStateAsync(task, Settings.SessionId)).ThrowIfError();

        task.State = state.State;
        task.Progress = state.Progress;
        task.Server = state.Server;
        JsonSettings.Default.Populate(state.Output.CreateReader(), output);

        return output.Link is not null;
    }
}
