using Common;
using MonoTorrent.Tracker;
using MonoTorrent.Tracker.Listeners;
using NLog;

Initializer.ConfigDirectory = "renderphin_tracker";
Init.Initialize();

var logger = LogManager.GetCurrentClassLogger();


var tracker = new TrackerServer()
{
    AllowUnregisteredTorrents = true,
    MinAnnounceInterval = TimeSpan.FromSeconds(1)
};
tracker.PeerAnnounced += (obj, e) => logger.Info($"PeerAnnounced {e.Peer.ClientAddress}");
tracker.PeerTimedOut += (obj, e) => logger.Info($"PeerTimedOut {e.Peer.ClientAddress}");
tracker.PeerScraped += (obj, e) => logger.Info($"PeerScraped {e.Torrents.Count}");

const int porth = 5120;
var listenerh = TrackerListenerFactory.CreateHttp($"http://t.microstock.plus:{porth}/announce/");
tracker.RegisterListener(listenerh);
listenerh.Start();

const int portu = 5121;
var listeneru = TrackerListenerFactory.CreateUdp(portu);
tracker.RegisterListener(listeneru);
listeneru.Start();

logger.Info("Tracker started on port http " + porth + " and udp " + portu);



Thread.Sleep(-1);