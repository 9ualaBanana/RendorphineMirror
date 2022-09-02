using MonoTorrent;
using Newtonsoft.Json;

namespace Common.Tasks.Model;

public class TorrentTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.Torrent;

    [Hidden] public string Link;
    [LocalFile] public readonly string Path;

    [JsonConstructor] public TorrentTaskInputInfo() => Link = Path = null!;
    public TorrentTaskInputInfo(string path, string link)
    {
        Path = path;
        Link = link;
    }

    async ValueTask ITaskInputInfo.InitializeAsync()
    {
        if (Link is not null) return;

        var (_, manager) = await TorrentClient.CreateAddTorrent(Path, true);
        Link = manager.MagnetLink.ToV1String();
    }
}
public class TorrentTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.Torrent;

    [LocalDirectory] public readonly string Directory;
    public readonly string FileName;

    public TorrentTaskOutputInfo(string directory, string fileName)
    {
        Directory = directory;
        FileName = fileName;
    }
}
