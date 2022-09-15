using MonoTorrent;

namespace Common.Tasks.Model;

public class TorrentTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.Torrent;

    [Hidden] public string? Link;
    [LocalFile] public readonly string Path;

    public TorrentTaskInputInfo(string path, string? link = null)
    {
        Path = path;
        Link = link;
    }

    async ValueTask ITaskInputOutputInfo.InitializeAsync()
    {
        if (Link is not null) return;

        var torrent = await Torrent.LoadAsync(await TorrentClient.CreateTorrent(Path));
        var magnet = new MagnetLink(torrent.InfoHash, torrent.Name, size: torrent.Size);
        Link = magnet.ToV1String();
    }
}
public class TorrentTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.Torrent;

    [Hidden] public string? Link;
}
