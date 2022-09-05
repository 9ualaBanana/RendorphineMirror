using MonoTorrent;

namespace Node.Tasks.Models;

public class TorrentTaskHandler : ITaskInputHandler, ITaskOutputHandler
{
    public TaskInputOutputType Type => TaskInputOutputType.Torrent;

    public async ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (TorrentTaskInputInfo) task.Input;

        if (task.ExecuteLocally || task.Info.LaunchPolicy == TaskPolicy.SameNode)
        {
            // TODO: remove after testin
            // return info.Path;
        }


        var dir = task.FSInputDirectory();
        var manager = await TorrentClient.StartMagnet(MagnetLink.FromUri(new Uri(info.Link)), dir);
        await TorrentClient.WaitForCompletion(manager, cancellationToken);

        return Directory.GetFiles(dir).Single();
    }
    public async ValueTask UploadResult(ReceivedTask task, string file, string? postfix, CancellationToken cancellationToken)
    {
        var info = (TorrentTaskOutputInfo) task.Output;

        if (task.ExecuteLocally || task.Info.LaunchPolicy == TaskPolicy.SameNode)
        {
            // TODO: ???? with .FileName & .Directory

            /*var filename = Path.GetFileNameWithoutExtension(info.FileName) + postfix + Path.GetExtension(info.FileName);

            Directory.CreateDirectory(info.Directory);
            File.Copy(file, Path.Combine(info.Directory, filename), true);
            return;*/
        }


        var (_, manager) = await TorrentClient.CreateAddTorrent(task.FSOutputDirectory(), true);

        var post = await Api.ApiPost($"{Api.TaskManagerEndpoint}/inittorrenttaskoutput", "Updating output magnet uri", ("sessionid", Settings.SessionId), ("taskid", task.Id), ("link", manager.MagnetLink.ToV1String()));
        post.ThrowIfError();

        info.Link = manager.MagnetLink.ToV1String();

        TorrentClient.SaveTorrent(manager);
    }
}
