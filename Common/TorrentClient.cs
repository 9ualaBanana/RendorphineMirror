using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Client.PortForwarding;

namespace Common
{
    public static class TorrentClient
    {
        public static int? DhtPort => Client.PortMappings.Created.FirstOrDefault(x => x.Protocol == Protocol.Udp)?.PublicPort;
        public static int? ListenPort => Client.PortMappings.Created.FirstOrDefault(x => x.Protocol == Protocol.Tcp)?.PublicPort;
        public static BEncodedString PeerId => Client.PeerId;

        static readonly TorrentCreator Creator = new TorrentCreator() { CreatedBy = "Renderphine v" + Init.Version };
        public static readonly ClientEngine Client;

        static TorrentClient()
        {
            var esettings = new EngineSettingsBuilder();
            Client = new ClientEngine();
        }


        static Task<BEncodedDictionary> CreateTorrent(string directory) => CreateTorrent(new TorrentFileSource(directory));
        static Task<BEncodedDictionary> CreateTorrent(ITorrentFileSource source) => Creator.CreateAsync(source);

        public static async Task<(byte[] data, TorrentManager manager)> CreateAddTorrent(string directory)
        {
            var data = await CreateTorrent(directory).ConfigureAwait(false);
            var encoded = data.Encode();
            var torrent = await Torrent.LoadAsync(encoded).ConfigureAwait(false);
            var manager = Client.Torrents.FirstOrDefault(x => x.InfoHash == torrent.InfoHash);
            if (manager is null) manager = await AddTorrent(torrent, Path.GetFullPath(Path.Combine(directory, ".."))).ConfigureAwait(false);

            return (encoded, manager);
        }

        public static async Task<TorrentManager> AddTorrent(Torrent torrent, string targetdir)
        {
            if (Client.DhtEngine.State != MonoTorrent.Dht.DhtState.Ready)
                _ = Client.DhtEngine.StartAsync();


            var manager = await Client.AddAsync(torrent, targetdir).ConfigureAwait(false);

            manager.PeersFound += (obj, e) =>
            {
                //if (e is DhtPeersAdded de)
                //    Console.WriteLine("DhtPeersAdded " + torrent.InfoHash.ToHex() + " " + de.NewPeers + string.Join(", ", manager.GetPeersAsync().Result.Select(x => x.Uri)));
                Console.WriteLine("PeersFound " + torrent.InfoHash.ToHex() + " " + e.GetType().Name + " " + e.NewPeers + " " + string.Join(", ", manager.GetPeersAsync().Result.Select(x => x.Uri)));
            };
            manager.PeerConnected += (obj, e) => Console.WriteLine("PeerConnected " + torrent.InfoHash.ToHex() + " " + e.Peer.Uri);
            manager.PeerDisconnected += (obj, e) => Console.WriteLine("PeerDisconnected " + torrent.InfoHash.ToHex() + " " + e.Peer.Uri);
            manager.TorrentStateChanged += (obj, e) => Console.WriteLine("TorrentStateChanged " + torrent.InfoHash.ToHex() + " " + e.NewState);
            manager.ConnectionAttemptFailed += (obj, e) => Console.WriteLine("ConnectionAttemptFailed " + torrent.InfoHash.ToHex() + " " + e);

            // manager.TrackerManager.AnnounceComplete += (obj, e) => Console.WriteLine("AnnounceComplete " + torrent.InfoHash.ToHex() + " " + e.Tracker.Uri + string.Join(", ", e.Peers.Select(x => x.ConnectionUri)));
            // manager.TrackerManager.ScrapeComplete += (obj, e) => Console.WriteLine("ScrapeComplete " + torrent.InfoHash.ToHex() + " " + e.Successful + " " + e.Tracker.Uri);

            await manager.HashCheckAsync(autoStart: true).ConfigureAwait(false);
            return manager;
        }
    }
}