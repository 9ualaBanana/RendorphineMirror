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


        public static Task<byte[]> CreateTorrent(string directory) => CreateTorrent(new TorrentFileSource(directory));
        public static async Task<byte[]> CreateTorrent(ITorrentFileSource source) => (await Creator.CreateAsync(source)).Encode();

        public static async Task<(byte[] data, TorrentManager manager)> CreateAddTorrent(string directory)
        {
            var data = await CreateTorrent(directory).ConfigureAwait(false);
            var torrent = await Torrent.LoadAsync(data).ConfigureAwait(false);
            var manager = Client.Torrents.FirstOrDefault(x => x.InfoHash == torrent.InfoHash);
            if (manager is null) manager = await AddOrGetTorrent(torrent, Path.GetFullPath(Path.Combine(directory, ".."))).ConfigureAwait(false);

            return (data, manager);
        }

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
    }
}