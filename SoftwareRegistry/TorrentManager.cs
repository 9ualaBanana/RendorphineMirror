namespace SoftwareRegistry;

public class TorrentManager
{
    readonly TorrentHolder Holder;
    readonly TorrentClient Client;
    readonly ILogger Logger;

    public TorrentManager(TorrentHolder holder, TorrentClient client, ILogger<TorrentManager> logger)
    {
        Holder = holder;
        Client = client;
        Logger = logger;
    }

    public async Task AddFromDirectory(string dir)
    {
        Directory.CreateDirectory(dir);

        foreach (var plugindir in Directory.GetDirectories(dir))
        {
            var plugin = Path.GetFileName(plugindir);

            foreach (var versiondir in Directory.GetDirectories(plugindir).Select(p => Path.GetRelativePath(plugindir, p)))
            {
                var version = Path.GetFileName(versiondir);

                Logger.LogInformation($"Creating torrent for {dir}");
                var bytes = await TorrentClient.CreateTorrent(dir);
                Holder.Add(plugin, version, bytes);

                var torrent = await Torrent.LoadAsync(bytes);
                var manager = await Client.AddOrGetTorrent(torrent, dir);
                Logger.LogInformation($"Started torrent {torrent.InfoHash.ToHex()}");
            }
        }
    }
}
