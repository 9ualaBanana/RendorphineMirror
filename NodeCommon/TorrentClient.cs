using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;

namespace NodeCommon;

public record JsonPeer(string PeerId, ImmutableArray<ushort> Ports);
public class TorrentClient
{
    readonly static Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly ImmutableArray<string> Trackers = ImmutableArray.Create(
        "http://t.microstock.plus:5120/announce/",
        "udp://t.microstock.plus:5121/"
    );
    static readonly TorrentCreator Creator;

    public readonly ushort DhtPort, ListenPort;
    public BEncodedString PeerId => Client.PeerId;
    public readonly ClientEngine Client;

    static TorrentClient()
    {
        Creator = new TorrentCreator()
        {
            CreatedBy = "Renderfin v" + Init.Version,
            Announces = { Trackers.ToList(), },
        };
    }
    public TorrentClient(ushort dhtport, ushort listenport)
    {
        DhtPort = dhtport;
        ListenPort = listenport;

        var esettings = new EngineSettingsBuilder()
        {
            DhtPort = DhtPort,
            ListenPort = ListenPort,
            CacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "cache"),
        };
        Client = new ClientEngine(esettings.ToSettings());
    }


    public async Task AddTrackers(TorrentManager manager, bool announce = false)
    {
        try
        {
            foreach (var tracker in Trackers)
                await manager.TrackerManager.AddTrackerAsync(new Uri(tracker));
        }
        catch (Exception ex) { LogManager.GetCurrentClassLogger().Error($"Could not add trackers to {manager.InfoHash.ToHex()}: {ex}"); }

        if (announce) await Announce(manager);
    }
    public async Task Announce(TorrentManager manager)
    {
        await manager.DhtAnnounceAsync();
        await manager.TrackerManager.AnnounceAsync(CancellationToken.None);
        await manager.TrackerManager.ScrapeAsync(CancellationToken.None);
    }
    void AddLoggers(TorrentManager manager)
    {
        manager.PeersFound += (obj, e) => Logger.Trace("PeersFound " + manager.InfoHash.ToHex() + " " + e.GetType().Name + " " + e.NewPeers + " " + string.Join(", ", manager.GetPeersAsync().Result.Select(x => x.Uri)));
        manager.PeerConnected += (obj, e) => Logger.Trace("PeerConnected " + manager.InfoHash.ToHex() + " " + e.Peer.Uri);
        manager.PeerDisconnected += (obj, e) => Logger.Trace("PeerDisconnected " + manager.InfoHash.ToHex() + " " + e.Peer.Uri);
        manager.TorrentStateChanged += (obj, e) => Logger.Trace("TorrentStateChanged " + manager.InfoHash.ToHex() + " " + e.NewState);
        manager.ConnectionAttemptFailed += (obj, e) => Logger.Trace("ConnectionAttemptFailed " + manager.InfoHash.ToHex() + " " + e.Peer.ConnectionUri + " " + e.Reason);
    }

    public Task<TorrentManager> StartMagnet(string magnet, string targetdir) => StartMagnet(MagnetLink.FromUri(new Uri(magnet)), targetdir);
    public async Task<TorrentManager> StartMagnet(MagnetLink magnet, string targetdir)
    {
        if (Client.DhtEngine.State == MonoTorrent.Dht.DhtState.NotReady)
            _ = Client.DhtEngine.StartAsync();

        Logger.Info($"Adding magnet {magnet.ToV1String()}");

        var manager = TryGetManager(magnet.InfoHash) ?? await Client.AddAsync(magnet, targetdir, new TorrentSettingsBuilder() { CreateContainingDirectory = false }.ToSettings());
        AddLoggers(manager);

        if (manager.State is not (TorrentState.Starting or TorrentState.Seeding or TorrentState.Downloading))
        {
            Logger.Info($"Starting magnet {magnet.ToV1String()}");

            if (manager.HasMetadata && !manager.Complete)
                await manager.HashCheckAsync(autoStart: true);
            else await manager.StartAsync();
        }
        return manager;
    }

    public static Task<byte[]> CreateTorrent(string path) => CreateTorrent(new TorrentFileSource(path));
    public static async Task<byte[]> CreateTorrent(ITorrentFileSource source) => (await Creator.CreateAsync(source)).Encode();

    public async Task<(byte[] data, TorrentManager manager)> CreateAddTorrent(string path)
    {
        var data = await CreateTorrent(path).ConfigureAwait(false);
        return await CreateAddTorrent(data, path);
    }
    public async Task<(byte[] data, TorrentManager manager)> CreateAddTorrent(byte[] data, string targetpath)
    {
        var torrent = await Torrent.LoadAsync(data).ConfigureAwait(false);
        Logger.Info($"Added torrent for {(File.Exists(targetpath) ? "file" : "directory")} {targetpath}: {torrent.InfoHash.ToHex()}");

        var manager = await AddOrGetTorrent(torrent, targetpath).ConfigureAwait(false);
        return (data, manager);
    }

    public TorrentManager? TryGetManager(Torrent torrent) => TryGetManager(torrent.InfoHash);
    public TorrentManager? TryGetManager(InfoHash hash) => Client.Torrents.FirstOrDefault(x => x.InfoHash == hash);

    public async Task<TorrentManager> AddOrGetTorrent(Torrent torrent, string targetdir)
    {
        if (TryGetManager(torrent.InfoHash) is { } manager)
            return manager;

        if (Client.DhtEngine.State == MonoTorrent.Dht.DhtState.NotReady)
            _ = Client.DhtEngine.StartAsync();

        Logger.Info($"Adding torrent {torrent.InfoHash.ToHex()}");

        manager = TryGetManager(torrent.InfoHash) ?? await Client.AddAsync(torrent, targetdir, new TorrentSettingsBuilder() { CreateContainingDirectory = false }.ToSettings());
        AddLoggers(manager);

        await manager.HashCheckAsync(autoStart: true).ConfigureAwait(false);
        Logger.Info($"{torrent.InfoHash.ToHex()} {manager.State}");

        return manager;
    }

    public async Task WaitForCompletion(TorrentManager manager, TimeoutCancellationToken token)
    {
        Logger.Info($"Waiting for download of {manager.InfoHash.ToHex()}");

        var progress = 0d;
        while (true)
        {
            if (manager.Complete) break;

            token.ThrowIfCancellationRequested();
            token.CheckStuck(ref progress, manager.Progress, $"Torrent task load was stuck at {manager.Progress}% ");
            progress = manager.Progress;

            await Task.Delay(2000);
        }

        await manager.StopAsync(TimeSpan.FromSeconds(10));
        Logger.Info($"Torrent {manager.InfoHash.ToHex()} was successfully downloaded, stopping");
    }
}