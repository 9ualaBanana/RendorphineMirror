using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;

namespace Common
{
    public record JsonPeer(string PeerId, ImmutableArray<ushort> Ports);
    public static class TorrentClient
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        static readonly string SavedTorrentsDirectory = Path.Combine(Init.ConfigDirectory, "torrents");

        public static ushort DhtPort => Settings.DhtPort;
        public static ushort ListenPort => Settings.TorrentPort;
        public static BEncodedString PeerId => Client.PeerId;

        static readonly TorrentCreator Creator = new TorrentCreator() { CreatedBy = "Renderphin v" + Init.Version };
        public static readonly ClientEngine Client;

        static TorrentClient()
        {
            var esettings = new EngineSettingsBuilder()
            {
                DhtPort = DhtPort,
                ListenPort = ListenPort,
            };
            Client = new ClientEngine(esettings.ToSettings());
        }

        public static async Task AddTrackers(TorrentManager manager, bool announce = false)
        {
            try
            {
                await manager.TrackerManager.AddTrackerAsync(new Uri("http://t.microstock.plus:5120/announce/"));
                await manager.TrackerManager.AddTrackerAsync(new Uri("udp://t.microstock.plus:5121/"));
            }
            catch (Exception ex) { LogManager.GetCurrentClassLogger().Error($"Could not add trackers to {manager.InfoHash.ToHex()}: {ex}"); }

            await manager.DhtAnnounceAsync();
            await manager.TrackerManager.AnnounceAsync(CancellationToken.None);
            await manager.TrackerManager.ScrapeAsync(CancellationToken.None);
        }

        public static Task<TorrentManager> StartMagnet(string magnet, string targetdir) => StartMagnet(MagnetLink.FromUri(new Uri(magnet)), targetdir);
        public static async Task<TorrentManager> StartMagnet(MagnetLink magnet, string targetdir)
        {
            var manager = TryGetManager(magnet.InfoHash) ?? await Client.AddAsync(magnet, targetdir);
            if (manager.State is not (TorrentState.Starting or TorrentState.Seeding or TorrentState.Downloading))
                await manager.StartAsync();

            return manager;
        }

        public static Task<byte[]> CreateTorrent(string directory) => CreateTorrent(new TorrentFileSource(directory));
        public static async Task<byte[]> CreateTorrent(ITorrentFileSource source) => (await Creator.CreateAsync(source)).Encode();

        public static async Task<(byte[] data, TorrentManager manager)> CreateAddTorrent(string directory)
        {
            var data = await CreateTorrent(directory).ConfigureAwait(false);
            var torrent = await Torrent.LoadAsync(data).ConfigureAwait(false);
            var manager = TryGetManager(torrent) ?? await AddOrGetTorrent(torrent, Path.GetFullPath(Path.Combine(directory, ".."))).ConfigureAwait(false);

            return (data, manager);
        }

        static TorrentManager? TryGetManager(Torrent torrent) => TryGetManager(torrent.InfoHash);
        static TorrentManager? TryGetManager(InfoHash hash) => Client.Torrents.FirstOrDefault(x => x.InfoHash == hash);

        public static TorrentManager? TryGet(InfoHash hash) => Client.Torrents.FirstOrDefault(x => x.InfoHash == hash);
        public static async Task<TorrentManager> AddOrGetTorrent(Torrent torrent, string targetdir)
        {
            if (Client.DhtEngine.State == MonoTorrent.Dht.DhtState.NotReady)
                _ = Client.DhtEngine.StartAsync();


            var manager = TryGet(torrent.InfoHash);
            if (manager is not null) return manager;

            manager = await Client.AddAsync(torrent, targetdir).ConfigureAwait(false);

            if (Init.IsDebug)
            {
                manager.PeersFound += (obj, e) => _logger.Trace("PeersFound " + torrent.InfoHash.ToHex() + " " + e.GetType().Name + " " + e.NewPeers + " " + string.Join(", ", manager.GetPeersAsync().Result.Select(x => x.Uri)));
                manager.PeerConnected += (obj, e) => _logger.Trace("PeerConnected " + torrent.InfoHash.ToHex() + " " + e.Peer.Uri);
                manager.PeerDisconnected += (obj, e) => _logger.Trace("PeerDisconnected " + torrent.InfoHash.ToHex() + " " + e.Peer.Uri);
                manager.TorrentStateChanged += (obj, e) => _logger.Trace("TorrentStateChanged " + torrent.InfoHash.ToHex() + " " + e.NewState);
                manager.ConnectionAttemptFailed += (obj, e) => _logger.Trace("ConnectionAttemptFailed " + torrent.InfoHash.ToHex() + " " + e.Peer.ConnectionUri + " " + e.Reason);
            }

            await manager.HashCheckAsync(autoStart: true).ConfigureAwait(false);
            return manager;
        }

        public static async Task WaitForCompletion(TorrentManager manager, CancellationToken token = default)
        {
            while (true)
            {
                if (token.IsCancellationRequested) return;

                await Task.Delay(2000);
                if (manager.Complete) break;
            }

            await manager.StopAsync(TimeSpan.FromSeconds(10));
        }
        public static async Task WaitForUploadCompletion(TorrentManager manager, CancellationToken token = default)
        {
            while (true)
            {
                if (token.IsCancellationRequested) return;

                await Task.Delay(2000);
                if (manager.Peers.Seeds > 0) break;
            }

            await manager.StopAsync(TimeSpan.FromSeconds(10));
        }
    }
}