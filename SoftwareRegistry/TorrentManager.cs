namespace SoftwareRegistry;

public class TorrentManager
{
    public string TorrentsDirectory { get; } = "torrents";

    readonly TorrentClient Client;
    readonly ILogger Logger;

    readonly Dictionary<string, (byte[] bytes, MonoTorrent.Client.TorrentManager manager)> Torrents = new();

    public TorrentManager(TorrentClient client, ILogger<TorrentManager> logger)
    {
        Client = client;
        Logger = logger;
    }


    static string ToKey(string plugin, string version) => $"{plugin}_{version}";
    public string DirectoryFor(string plugin, string version) => Path.Combine(TorrentsDirectory, plugin, version);

    public bool TryGetBytes(string plugin, string version, [NotNullWhen(true)] out byte[]? bytes)
    {
        if (Torrents.TryGetValue(ToKey(plugin, version), out var data))
        {
            bytes = data.bytes;
            return true;
        }

        bytes = null;
        return false;
    }

    public async Task DeleteAsync(string plugin, string version)
    {
        var dir = DirectoryFor(plugin, version);

        if (Torrents.TryGetValue(ToKey(plugin, version), out var data))
        {
            await data.manager.StopAsync(TimeSpan.FromSeconds(5));
            await Client.Client.RemoveAsync(data.manager, MonoTorrent.Client.RemoveMode.CacheDataAndDownloadedData);
        }

        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    public async Task AddAsync(string plugin, string version) =>
        await AddAsync(plugin, version, DirectoryFor(plugin, version));

    public async Task AddAsync(string plugin, string version, string dir)
    {
        Logger.LogInformation($"Creating torrent for {dir}");
        var bytes = await TorrentClient.CreateTorrent(dir);

        var torrent = await Torrent.LoadAsync(bytes);
        var manager = await Client.AddOrGetTorrent(torrent, dir);

        Torrents.Add(ToKey(plugin, version), (bytes, manager));
        Logger.LogInformation($"Started torrent {torrent.InfoHash.ToHex()}");
    }

    public async Task AddFromMainDirectoryAsync()
    {
        var dir = TorrentsDirectory;
        Directory.CreateDirectory(dir);

        foreach (var plugindir in Directory.GetDirectories(dir))
        {
            var plugin = Path.GetFileName(plugindir);

            foreach (var versiondir in Directory.GetDirectories(plugindir))
            {
                var version = Path.GetFileName(versiondir);
                await AddAsync(plugin, version, versiondir);
            }
        }
    }
}
