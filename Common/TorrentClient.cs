using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;

namespace Common
{
    public record JsonPeer(string PeerId, ImmutableArray<ushort> Ports);
    public static class TorrentClient
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public static ushort DhtPort => Settings.DhtPort;
        public static ushort ListenPort => Settings.TorrentPort;
        public static BEncodedString PeerId => Client.PeerId;

        static readonly TorrentCreator Creator = new TorrentCreator() { CreatedBy = "Renderfin v" + Init.Version };
        public static readonly ClientEngine Client;

        static TorrentClient()
        {
            Creator = new TorrentCreator()
            {
                CreatedBy = "Renderfin v" + Init.Version,
                Announces = { Trackers.ToList(), },
            };

            var esettings = new EngineSettingsBuilder()
            {
                DhtPort = DhtPort,
                ListenPort = ListenPort,
                CacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "cache"),
            };
            Client = new ClientEngine(esettings.ToSettings());
        }

        public static readonly ImmutableArray<string> Trackers = ImmutableArray.Create(
            "http://t.microstock.plus:5120/announce/",
            "udp://t.microstock.plus:5121/"
        );


        public static async Task AddTrackers(TorrentManager manager, bool announce = false)
        {
            try
            {
                foreach (var tracker in Trackers)
                    await manager.TrackerManager.AddTrackerAsync(new Uri(tracker));
            }
            catch (Exception ex) { LogManager.GetCurrentClassLogger().Error($"Could not add trackers to {manager.InfoHash.ToHex()}: {ex}"); }

            if (announce) await Announce(manager);
        }
        public static async Task Announce(TorrentManager manager)
        {
            await manager.DhtAnnounceAsync();
            await manager.TrackerManager.AnnounceAsync(CancellationToken.None);
            await manager.TrackerManager.ScrapeAsync(CancellationToken.None);
        }
        static void AddLoggers(TorrentManager manager)
        {
            //if (Init.IsDebug)
            {
                manager.PeersFound += (obj, e) => _logger.Trace("PeersFound " + manager.InfoHash.ToHex() + " " + e.GetType().Name + " " + e.NewPeers + " " + string.Join(", ", manager.GetPeersAsync().Result.Select(x => x.Uri)));
                manager.PeerConnected += (obj, e) => _logger.Trace("PeerConnected " + manager.InfoHash.ToHex() + " " + e.Peer.Uri);
                manager.PeerDisconnected += (obj, e) => _logger.Trace("PeerDisconnected " + manager.InfoHash.ToHex() + " " + e.Peer.Uri);
                manager.TorrentStateChanged += (obj, e) => _logger.Trace("TorrentStateChanged " + manager.InfoHash.ToHex() + " " + e.NewState);
                manager.ConnectionAttemptFailed += (obj, e) => _logger.Trace("ConnectionAttemptFailed " + manager.InfoHash.ToHex() + " " + e.Peer.ConnectionUri + " " + e.Reason);
            }
        }

        public static Task<TorrentManager> StartMagnet(string magnet, string targetdir) => StartMagnet(MagnetLink.FromUri(new Uri(magnet)), targetdir);
        public static async Task<TorrentManager> StartMagnet(MagnetLink magnet, string targetdir)
        {
            if (Client.DhtEngine.State == MonoTorrent.Dht.DhtState.NotReady)
                _ = Client.DhtEngine.StartAsync();

            _logger.Info($"Adding magnet {magnet.ToV1String()}");

            var manager = TryGetManager(magnet.InfoHash) ?? await Client.AddAsync(magnet, targetdir);
            AddLoggers(manager);

            if (manager.State is not (TorrentState.Starting or TorrentState.Seeding or TorrentState.Downloading))
            {
                _logger.Info($"Starting magnet {magnet.ToV1String()}");

                if (manager.HasMetadata && !manager.Complete)
                    await manager.HashCheckAsync(autoStart: true);
                else await manager.StartAsync();
            }
            return manager;
        }

        public static Task<byte[]> CreateTorrent(string path) => CreateTorrent(new TorrentFileSource(path));
        public static async Task<byte[]> CreateTorrent(ITorrentFileSource source) => (await Creator.CreateAsync(source)).Encode();

        public static async Task<(byte[] data, TorrentManager manager)> CreateAddTorrent(string path)
        {
            var data = await CreateTorrent(path).ConfigureAwait(false);
            var torrent = await Torrent.LoadAsync(data).ConfigureAwait(false);

            _logger.Info($"Added torrent for {(File.Exists(path) ? "file" : "directory")} {path}: {torrent.InfoHash.ToHex()}");

            // i don't know why this behaves differently but it does
            var target = path;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                target = Path.Combine(path, "..");
            else target = File.Exists(path) ? Path.Combine(path, "..") : path;

            var manager = await AddOrGetTorrent(torrent, target).ConfigureAwait(false);
            return (data, manager);
        }

        public static TorrentManager? TryGetManager(Torrent torrent) => TryGetManager(torrent.InfoHash);
        public static TorrentManager? TryGetManager(InfoHash hash) => Client.Torrents.FirstOrDefault(x => x.InfoHash == hash);

        public static async Task<TorrentManager> AddOrGetTorrent(Torrent torrent, string targetdir)
        {
            if (TryGetManager(torrent.InfoHash) is { } manager)
                return manager;

            if (Client.DhtEngine.State == MonoTorrent.Dht.DhtState.NotReady)
                _ = Client.DhtEngine.StartAsync();

            _logger.Info($"Adding torrent {torrent.InfoHash.ToHex()}");

            manager = TryGetManager(torrent.InfoHash) ?? await Client.AddAsync(torrent, targetdir);
            AddLoggers(manager);

            await manager.HashCheckAsync(autoStart: true).ConfigureAwait(false);
            _logger.Info($"{torrent.InfoHash.ToHex()} {manager.State}");

            return manager;
        }

        public static async Task WaitForCompletion(TorrentManager manager, StuckCancellationToken token)
        {
            _logger.Info($"Waiting for download of {manager.InfoHash.ToHex()}");

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
            _logger.Info($"Torrent {manager.InfoHash.ToHex()} was successfully downloaded, stopping");
        }
    }
}