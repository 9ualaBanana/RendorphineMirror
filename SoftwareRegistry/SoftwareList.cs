namespace SoftwareRegistry;

public class SoftwareList
{
    public IEnumerable<SoftwareVersionInfo> AllSoftware => Software.Values.Select(s => s.Info);
    readonly Dictionary<(PluginType plugin, string version), StoredSoftware> Software = new();

    readonly TorrentClient TorrentClient;
    readonly ILogger Logger;

    public SoftwareList(TorrentClient torrentClient, ILogger<SoftwareList> logger)
    {
        TorrentClient = torrentClient;
        Logger = logger;
    }


    public bool TryGet(PluginType plugin, string version, [NotNullWhen(true)] out StoredSoftware? info) =>
        Software.TryGetValue((plugin, version), out info);

    public async Task<bool> RemoveAsync(PluginType plugin, string version)
    {
        if (!Software.TryGetValue((plugin, version), out var soft))
            return false;

        Software.Remove((plugin, version));
        await soft.TorrentManager.StopAsync(TimeSpan.FromSeconds(5));
        await TorrentClient.Client.RemoveAsync(soft.TorrentManager, MonoTorrent.Client.RemoveMode.CacheDataAndDownloadedData);
        Directory.Delete(soft.Directory, true);

        return true;
    }


    static OperationResult<SoftwareVersionInfo> TryReadPluginJson(string dir)
    {
        var infofile = Path.Combine(dir, "plugin.json");
        if (!File.Exists(infofile))
            return OperationResult.Err("plugin.json was not found");

        var definition = JsonConvert.DeserializeObject<SoftwareVersionInfo>(File.ReadAllText(infofile));
        if (definition is null)
            return OperationResult.Err("Could not deserialize plugin.json");

        return definition.AsOpResult();
    }

    public async Task AddPluginsFromDirectory(string directory)
    {
        foreach (var dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
            await TryAddPlugin(dir);
    }
    async Task<OperationResult> TryAddPlugin(string dir)
    {
        var def = TryReadPluginJson(dir);
        if (!def) return def.GetResult();
        var definition = def.Value;

        await Add(dir, definition);
        return true;
    }

    public async Task TryAddNewPlugin(SoftwareVersionInfo definition, string dir)
    {
        await RemoveAsync(definition.Type, definition.Version);

        var newdir = Path.Combine("plugins", definition.Type.ToString().ToLowerInvariant(), definition.Version);
        if (Directory.Exists(newdir))
            Directory.Delete(newdir, true);

        Directory.CreateDirectory(Path.GetDirectoryName(newdir)!);
        Directory.Move(dir, newdir);
        dir = newdir;

        await Add(dir, definition);
    }
    public async Task<OperationResult> TryAddNewPlugin(string dir)
    {
        var def = TryReadPluginJson(dir);
        if (!def) return def.GetResult();
        var definition = def.Value;

        await TryAddNewPlugin(definition, dir);
        return true;
    }
    async Task Add(string dir, SoftwareVersionInfo definition)
    {
        Logger.LogInformation($"Adding plugin {definition.Type} {definition.Version} {dir}");
        var bytes = await TorrentClient.CreateTorrent(dir);
        var torrent = await Torrent.LoadAsync(bytes);
        var manager = await TorrentClient.AddOrGetTorrent(torrent, dir);

        var info = new StoredSoftware(dir, definition, bytes, manager);
        Software.Add((definition.Type, definition.Version), info);
    }


    public class StoredSoftware
    {
        public string Directory { get; }
        public SoftwareVersionInfo Info { get; }
        public byte[] TorrentFileBytes { get; }
        public MonoTorrent.Client.TorrentManager TorrentManager { get; }

        public StoredSoftware(string directory, SoftwareVersionInfo info, byte[] torrentFileBytes, MonoTorrent.Client.TorrentManager torrentManager)
        {
            Directory = directory;
            Info = info;
            TorrentFileBytes = torrentFileBytes;
            TorrentManager = torrentManager;
        }
    }
}
