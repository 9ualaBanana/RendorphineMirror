using MonoTorrent;

namespace NodeCommon.Tasks.Model;

public class TorrentTaskInputInfo : ITaskInputFileInfo
{
    public TaskInputType Type => TaskInputType.Torrent;

    [Hidden] public string? Link;
    [LocalFile, NonSerializableForTasks] public readonly string Path;

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

    public ValueTask<TaskObject> GetFileInfo()
    {
        if (File.Exists(Path)) return get(Path).AsVTask();
        return get(Directory.GetFiles(Path, "*", SearchOption.AllDirectories).First()).AsVTask();


        TaskObject get(string file) => new TaskObject(System.IO.Path.GetFileName(file), new FileInfo(file).Length);
    }
}
public class TorrentTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.Torrent;

    [Hidden] public string? Link;
}
