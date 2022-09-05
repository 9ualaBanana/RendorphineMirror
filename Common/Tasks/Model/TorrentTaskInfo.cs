namespace Common.Tasks.Model;

public class TorrentTaskInputInfo : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.Torrent;

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

        var (_, manager) = await TorrentClient.CreateAddTorrent(Path, true);
        Link = manager.MagnetLink.ToV1String();
    }
}
public class TorrentTaskOutputInfo : ITaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.Torrent;

    [Hidden] public string? Link;
}
